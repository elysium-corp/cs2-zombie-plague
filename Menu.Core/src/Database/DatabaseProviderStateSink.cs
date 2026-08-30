using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Channels;
using Menu.Api.Providers;
using Menu.Core.Configuration;
using Menu.Core.Database.Models;
using Menu.Core.Database.Repositories;
using Menu.Core.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Menu.Core.Database;

/// <summary>
/// Неблокирующий best-effort мост из plugin lifecycle в PostgreSQL.
/// Актуальный снимок остаётся в памяти и периодически reconciles после временного сбоя БД.
/// </summary>
internal sealed class DatabaseProviderStateSink : IProviderStateSink, IDisposable, IAsyncDisposable
{
    private static readonly TimeSpan ShutdownFlushTimeout = TimeSpan.FromSeconds(5);
    private readonly ProviderStateRepository _repository;
    private readonly ILogger<DatabaseProviderStateSink> _logger;
    private readonly string _serverKey;
    private readonly TimeSpan _reconcileInterval;
    private readonly ConcurrentDictionary<string, ProviderAccumulator> _providers =
        new(StringComparer.Ordinal);
    private readonly ConcurrentDictionary<(string ProviderKey, Guid SessionId, long Generation), byte>
        _pendingOffline = new();
    private readonly object _stateGate = new();
    private readonly Channel<PersistenceEvent> _events = Channel.CreateUnbounded<PersistenceEvent>(
        new UnboundedChannelOptions { SingleReader = true, AllowSynchronousContinuations = false });
    private readonly CancellationTokenSource _timerStop = new();
    private readonly Task _worker;
    private readonly Task _timer;
    private int _disposed;

    public DatabaseProviderStateSink(
        ProviderStateRepository repository,
        IOptions<MenuCoreConfig> options,
        ILogger<DatabaseProviderStateSink> logger)
    {
        _repository = repository;
        _logger = logger;
        _serverKey = options.Value.ServerKey;
        _reconcileInterval = TimeSpan.FromSeconds(options.Value.SyncIntervalSeconds);
        _worker = Task.Run(ProcessAsync);
        _timer = Task.Run(() => QueuePeriodicReconcileAsync(_timerStop.Token));
    }

    public void ProviderRegistered(MenuProviderDescriptor descriptor, Guid sessionId, long generation)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        try
        {
            var state = ProviderAccumulator.Create(
                descriptor,
                sessionId,
                generation,
                SerializeOrFallback(descriptor.Capabilities, "[]", descriptor.ProviderKey),
                SerializeOrFallback(descriptor.Metadata, "{}", descriptor.ProviderKey),
                MenuDatabaseValues.ProviderStatusOnline,
                null);
            lock (_stateGate)
            {
                _providers.AddOrUpdate(descriptor.ProviderKey, state, (_, _) => state);
                TryQueueSnapshot(state, sessionId, generation);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Не удалось сериализовать Provider {ProviderKey} для persistence.", descriptor.ProviderKey);
        }
    }

    public void ProviderRejected(
        MenuProviderDescriptor descriptor,
        Guid sessionId,
        long generation,
        ProviderRejectionStatus status,
        string errorCode)
    {
        if (Volatile.Read(ref _disposed) != 0)
        {
            return;
        }

        try
        {
            var databaseStatus = status switch
            {
                ProviderRejectionStatus.ApiOutdated => MenuDatabaseValues.ProviderStatusApiOutdated,
                ProviderRejectionStatus.Incompatible => MenuDatabaseValues.ProviderStatusIncompatible,
                _ => MenuDatabaseValues.ProviderStatusError
            };
            var state = ProviderAccumulator.Create(
                descriptor,
                sessionId,
                generation,
                SerializeOrFallback(descriptor.Capabilities, "[]", descriptor.ProviderKey),
                SerializeOrFallback(descriptor.Metadata, "{}", descriptor.ProviderKey),
                databaseStatus,
                errorCode);
            lock (_stateGate)
            {
                _providers.AddOrUpdate(descriptor.ProviderKey, state, (_, _) => state);
                TryQueueSnapshot(state, sessionId, generation);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Не удалось сериализовать отклонённый Provider {ProviderKey} для persistence.",
                descriptor.ProviderKey);
        }
    }

    public void MenuDeclared(
        string providerKey,
        Guid sessionId,
        long generation,
        MenuProviderMenuDescriptor descriptor)
    {
        TryUpdateExport(
            providerKey,
            sessionId,
            generation,
            new MenuProviderExportPersistence(
                MenuDatabaseValues.ExportTypeMenu,
                descriptor.MenuKey,
                descriptor.DisplayName.Default,
                null,
                SerializeExportMetadata(descriptor.DisplayName, descriptor.Description, descriptor.Metadata)));
    }

    public void ActionDeclared(
        string providerKey,
        Guid sessionId,
        long generation,
        MenuProviderActionDescriptor descriptor)
    {
        string? schema = null;
        try
        {
            schema = descriptor.ArgumentsSchema?.GetRawText();
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Некорректная JSON Schema Provider action {ProviderKey}/{ActionKey}.", providerKey, descriptor.ActionKey);
        }

        TryUpdateExport(
            providerKey,
            sessionId,
            generation,
            new MenuProviderExportPersistence(
                MenuDatabaseValues.ExportTypeAction,
                descriptor.ActionKey,
                descriptor.DisplayName.Default,
                schema,
                SerializeExportMetadata(descriptor.DisplayName, descriptor.Description, descriptor.Metadata)));
    }

    public void ExportRemoved(
        string providerKey,
        Guid sessionId,
        long generation,
        string exportType,
        string exportKey)
    {
        lock (_stateGate)
        {
            if (_providers.TryGetValue(providerKey, out var state)
                && state.TryRemoveExport(sessionId, generation, exportType, exportKey))
            {
                TryQueueSnapshot(state, sessionId, generation);
            }
        }
    }

    public void ProviderOffline(string providerKey, Guid sessionId, long generation)
    {
        lock (_stateGate)
        {
            if (_providers.TryGetValue(providerKey, out var state)
                && state.IsSession(sessionId, generation)
                && ((ICollection<KeyValuePair<string, ProviderAccumulator>>)_providers).Remove(
                    new KeyValuePair<string, ProviderAccumulator>(providerKey, state)))
            {
                _pendingOffline.TryAdd((providerKey, sessionId, generation), 0);
                _events.Writer.TryWrite(new OfflineEvent(providerKey, sessionId, generation));
            }
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _timerStop.Cancel();
        _events.Writer.TryComplete();

        try
        {
            if (!Task.WhenAll(_worker, _timer).Wait(ShutdownFlushTimeout))
            {
                _logger.LogWarning(
                    "Provider persistence queue не успела полностью сброситься за {TimeoutSeconds} секунд.",
                    ShutdownFlushTimeout.TotalSeconds);
            }
        }
        catch (AggregateException exception)
        {
            _logger.LogWarning(
                exception.Flatten(),
                "Ошибка bounded flush Provider persistence queue при остановке.");
        }
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        try
        {
            await Task.WhenAll(_worker, _timer).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Ожидаемая остановка plugin lifecycle.
        }
        finally
        {
            _timerStop.Dispose();
        }
    }

    private void TryUpdateExport(
        string providerKey,
        Guid sessionId,
        long generation,
        MenuProviderExportPersistence export)
    {
        lock (_stateGate)
        {
            if (_providers.TryGetValue(providerKey, out var state)
                && state.TrySetExport(sessionId, generation, export))
            {
                TryQueueSnapshot(state, sessionId, generation);
            }
        }
    }

    private void TryQueueSnapshot(ProviderAccumulator state, Guid sessionId, long generation)
    {
        if (state.TrySnapshot(sessionId, generation, out var snapshot))
        {
            _events.Writer.TryWrite(new ReconcileEvent(snapshot));
        }
    }

    private async Task ProcessAsync()
    {
        await foreach (var persistenceEvent in _events.Reader.ReadAllAsync())
        {
            try
            {
                switch (persistenceEvent)
                {
                    case ReconcileEvent reconcile:
                        await _repository.ReconcileAsync(
                                _serverKey,
                                reconcile.Snapshot,
                                CancellationToken.None)
                            .ConfigureAwait(false);
                        break;

                    case OfflineEvent offline:
                        await _repository.MarkOfflineAsync(
                                _serverKey,
                                offline.ProviderKey,
                                offline.SessionId,
                                offline.Generation,
                                CancellationToken.None)
                            .ConfigureAwait(false);
                        _pendingOffline.TryRemove(
                            (offline.ProviderKey, offline.SessionId, offline.Generation),
                            out _);
                        break;
                }
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Best-effort Provider persistence event {EventType} не сохранён; reconcile повторится.",
                    persistenceEvent.GetType().Name);
            }
        }
    }

    private async Task QueuePeriodicReconcileAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(_reconcileInterval);
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                lock (_stateGate)
                {
                    foreach (var pending in _pendingOffline.Keys)
                    {
                        _events.Writer.TryWrite(new OfflineEvent(
                            pending.ProviderKey,
                            pending.SessionId,
                            pending.Generation));
                    }

                    foreach (var state in _providers.Values)
                    {
                        var identity = state.GetIdentity();
                        TryQueueSnapshot(state, identity.SessionId, identity.Generation);
                    }
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ожидаемая остановка.
        }
    }

    private string SerializeExportMetadata(
        object displayName,
        object? description,
        IReadOnlyDictionary<string, JsonElement> metadata) =>
        SerializeOrFallback(new { displayName, description, metadata }, "{}", "provider_export");

    private string SerializeOrFallback(object value, string fallback, string key)
    {
        try
        {
            return JsonSerializer.Serialize(value);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "JSON metadata {MetadataKey} не сериализована; используется безопасный fallback.",
                key);
            return fallback;
        }
    }

    private abstract record PersistenceEvent(string ProviderKey, Guid SessionId, long Generation);

    private sealed record ReconcileEvent(MenuProviderPersistenceSnapshot Snapshot)
        : PersistenceEvent(Snapshot.ProviderKey, Snapshot.SessionId, Snapshot.Generation);

    private sealed record OfflineEvent(string ProviderKey, Guid SessionId, long Generation)
        : PersistenceEvent(ProviderKey, SessionId, Generation);

    private sealed class ProviderAccumulator
    {
        private readonly object _gate = new();
        private readonly Dictionary<(string Type, string Key), MenuProviderExportPersistence> _exports = [];
        private readonly string _providerKey;
        private readonly string _displayName;
        private readonly string _pluginVersion;
        private readonly int _menuApiVersion;
        private readonly string _capabilitiesJson;
        private readonly string _metadataJson;
        private readonly string _status;
        private readonly string? _lastError;
        private readonly Guid _sessionId;
        private readonly long _generation;

        private ProviderAccumulator(
            MenuProviderDescriptor descriptor,
            Guid sessionId,
            long generation,
            string capabilitiesJson,
            string metadataJson,
            string status,
            string? lastError)
        {
            _providerKey = descriptor.ProviderKey;
            _displayName = descriptor.DisplayName;
            _pluginVersion = descriptor.PluginVersion;
            _menuApiVersion = descriptor.MenuApiVersion;
            _capabilitiesJson = capabilitiesJson;
            _metadataJson = metadataJson;
            _status = status;
            _lastError = lastError;
            _sessionId = sessionId;
            _generation = generation;
        }

        internal static ProviderAccumulator Create(
            MenuProviderDescriptor descriptor,
            Guid sessionId,
            long generation,
            string capabilitiesJson,
            string metadataJson,
            string status,
            string? lastError) =>
            new(descriptor, sessionId, generation, capabilitiesJson, metadataJson, status, lastError);

        internal bool IsSession(Guid sessionId, long generation) =>
            _sessionId == sessionId && _generation == generation;

        internal (string ProviderKey, Guid SessionId, long Generation) GetIdentity() =>
            (_providerKey, _sessionId, _generation);

        internal bool TrySetExport(
            Guid sessionId,
            long generation,
            MenuProviderExportPersistence export)
        {
            if (!IsSession(sessionId, generation))
            {
                return false;
            }

            lock (_gate)
            {
                _exports[(export.ExportType, export.ExportKey)] = export;
            }

            return true;
        }

        internal bool TryRemoveExport(
            Guid sessionId,
            long generation,
            string exportType,
            string exportKey)
        {
            if (!IsSession(sessionId, generation))
            {
                return false;
            }

            lock (_gate)
            {
                return _exports.Remove((exportType, exportKey));
            }
        }

        internal bool TrySnapshot(
            Guid sessionId,
            long generation,
            out MenuProviderPersistenceSnapshot snapshot)
        {
            if (!IsSession(sessionId, generation))
            {
                snapshot = null!;
                return false;
            }

            lock (_gate)
            {
                snapshot = new MenuProviderPersistenceSnapshot(
                    _providerKey,
                    _displayName,
                    _pluginVersion,
                    _menuApiVersion,
                    _capabilitiesJson,
                    _metadataJson,
                    _status,
                    _lastError,
                    _sessionId,
                    _generation,
                    _exports.Values.ToArray());
            }

            return true;
        }
    }
}
