using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Core.Configuration;
using Menu.Core.Commands;
using Menu.Core.Database.Models;
using Menu.Core.Database.Repositories;
using Menu.Core.Runtime;
using Menu.Core.Storage;
using Menu.Core.Swiftly;
using Menu.Core.Validation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace Menu.Core.Application;

internal sealed record MenuReloadResult(
    bool Activated,
    long ActiveReleaseId,
    MenuSnapshotSource Source,
    string Message);

/// <summary>
/// Загружает Release на cold path, валидирует целиком и выполняет один atomic swap.
/// </summary>
internal sealed class MenuSyncCoordinator : IDisposable
{
    private readonly ISwiftlyCore _core;
    private readonly MenuReleaseRepository _releases;
    private readonly ProviderStateRepository _providerStates;
    private readonly MenuRuntimeStatusRepository _statuses;
    private readonly MenuSnapshotStore _snapshots;
    private readonly MenuReleaseFileStore _files;
    private readonly MenuBootstrapLoader _bootstrap;
    private readonly MenuValidationContextFactory _contextFactory;
    private readonly MenuCapabilityProvider _capabilities;
    private readonly MenuCommandRouter _commandRouter;
    private readonly MenuCoreConfig _configuration;
    private readonly ILogger<MenuSyncCoordinator> _logger;
    private readonly SemaphoreSlim _reloadGate = new(1, 1);
    private readonly CancellationTokenSource _stop = new();
    private Task? _worker;
    private MenuRuntimeStatusLease? _statusLease;
    private DateTimeOffset? _lastDatabaseSyncAt;
    private long? _lastKnownGoodReleaseId;
    private long? _fallbackReleaseId;
    private string? _lastError;
    private int _started;
    private int _disposed;

    internal MenuSyncCoordinator(
        ISwiftlyCore core,
        MenuReleaseRepository releases,
        ProviderStateRepository providerStates,
        MenuRuntimeStatusRepository statuses,
        MenuSnapshotStore snapshots,
        MenuReleaseFileStore files,
        MenuBootstrapLoader bootstrap,
        MenuValidationContextFactory contextFactory,
        MenuCapabilityProvider capabilities,
        MenuCommandRouter commandRouter,
        IOptions<MenuCoreConfig> options,
        ILogger<MenuSyncCoordinator> logger)
    {
        _core = core;
        _releases = releases;
        _providerStates = providerStates;
        _statuses = statuses;
        _snapshots = snapshots;
        _files = files;
        _bootstrap = bootstrap;
        _contextFactory = contextFactory;
        _capabilities = capabilities;
        _commandRouter = commandRouter;
        _configuration = options.Value;
        _logger = logger;
        LastKnownGoodPath = ResolveDataFile(core.PluginDataDirectory, _configuration.LastKnownGoodFileName);
        FallbackPath = ResolveDataFile(core.PluginDataDirectory, _configuration.FallbackFileName);
    }

    internal string LastKnownGoodPath { get; }

    internal string FallbackPath { get; }

    internal void Start()
    {
        if (Interlocked.Exchange(ref _started, 1) != 0)
        {
            return;
        }

        _worker = Task.Run(() => RunAsync(_stop.Token));
    }

    internal async Task<MenuReloadResult> ReloadNowAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _stop.Token);
        await _reloadGate.WaitAsync(linked.Token).ConfigureAwait(false);
        try
        {
            return await ReloadDatabaseCoreAsync(linked.Token).ConfigureAwait(false);
        }
        finally
        {
            _reloadGate.Release();
        }
    }

    internal async Task StopAsync(TimeSpan timeout)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
        {
            return;
        }

        _stop.Cancel();
        using var timeoutSource = new CancellationTokenSource(timeout);
        if (_worker is not null)
        {
            try
            {
                await _worker.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
            {
                _logger.LogWarning("Menu.Core sync worker не завершился за {Timeout}.", timeout);
            }
            catch (OperationCanceledException)
            {
                // Ожидаемая отмена lifecycle-задачи.
            }
        }

        try
        {
            await _reloadGate.WaitAsync(timeoutSource.Token).ConfigureAwait(false);
            _reloadGate.Release();
        }
        catch (OperationCanceledException) when (timeoutSource.IsCancellationRequested)
        {
            _logger.LogWarning("Menu.Core manual reload не завершился за {Timeout}.", timeout);
        }
    }

    public void Dispose()
    {
        _stop.Cancel();
        // Эти primitives намеренно не освобождаются здесь: bounded shutdown может
        // завершиться по timeout, а уже начатый reload всё ещё обязан безопасно
        // выйти из finally и освободить gate.
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        await EnsureStatusLeaseAsync(cancellationToken).ConfigureAwait(false);

        var database = await ReloadSafelyAsync(cancellationToken).ConfigureAwait(false);
        if (!database.Activated && _snapshots.Current.Source == MenuSnapshotSource.None)
        {
            await ActivateLocalAsync(cancellationToken).ConfigureAwait(false);
        }

        await UpdateStatusSafelyAsync(cancellationToken).ConfigureAwait(false);

        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(_configuration.SyncIntervalSeconds));
        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
            {
                await EnsureStatusLeaseAsync(cancellationToken).ConfigureAwait(false);
                await ReloadSafelyAsync(cancellationToken).ConfigureAwait(false);
                await UpdateStatusSafelyAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Ожидаемая отмена lifecycle-задачи.
        }
    }

    private async Task<MenuReloadResult> ReloadSafelyAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await ReloadNowAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            _lastError = Truncate(exception.Message, 2048);
            _logger.LogWarning(exception,
                "Menu.Core не смог синхронизировать PostgreSQL; active snapshot сохранён.");
            var current = _snapshots.Current;
            return new MenuReloadResult(false, current.ReleaseId, current.Source, "database_unavailable");
        }
    }

    private async Task<MenuReloadResult> ReloadDatabaseCoreAsync(CancellationToken cancellationToken)
    {
        var target = await _releases.LoadActiveTargetAsync(
                _configuration.ServerKey,
                cancellationToken)
            .ConfigureAwait(false);
        _lastDatabaseSyncAt = DateTimeOffset.UtcNow;

        if (target is null)
        {
            _lastError = "active_release_not_configured";
            RecordDatabaseRejection(
                "database.active_release_missing",
                "No active release target is configured for this server.");
            var current = _snapshots.Current;
            return new MenuReloadResult(false, current.ReleaseId, current.Source, _lastError);
        }

        var currentSnapshot = _snapshots.Current;
        if (currentSnapshot.Source == MenuSnapshotSource.Database
            && currentSnapshot.ReleaseId == target.ReleaseId
            && MenuJson.FixedTimeChecksumEquals(target.Checksum, currentSnapshot.Checksum))
        {
            _lastError = null;
            return new MenuReloadResult(false, currentSnapshot.ReleaseId, currentSnapshot.Source, "release_unchanged");
        }

        MenuReleaseDefinition? release;
        try
        {
            release = MenuJson.DeserializeRelease(target.ArtifactJson);
        }
        catch (JsonException exception)
        {
            _lastError = "database_artifact_json_invalid";
            RecordDatabaseRejection(
                "database.artifact_json_invalid",
                "The active database release contains invalid or duplicate-property JSON.");
            _logger.LogError(exception, "Release target {ReleaseId}/{ServerKey} содержит некорректный JSON.",
                target.ReleaseId, target.ServerKey);
            return new MenuReloadResult(false, currentSnapshot.ReleaseId, currentSnapshot.Source, _lastError);
        }

        if (release is null
            || release.ReleaseId != target.ReleaseId
            || !MenuJson.FixedTimeChecksumEquals(target.Checksum, release.Checksum ?? string.Empty))
        {
            _lastError = "database_artifact_metadata_mismatch";
            RecordDatabaseRejection(
                "database.artifact_metadata_mismatch",
                "The active database release ID or checksum does not match its target metadata.");
            return new MenuReloadResult(false, currentSnapshot.ReleaseId, currentSnapshot.Source, _lastError);
        }

        var persistedProviders = await _providerStates.LoadValidationEntriesAsync(
                _configuration.ServerKey,
                cancellationToken)
            .ConfigureAwait(false);
        var activation = _snapshots.TryActivate(
            release,
            _contextFactory.Create(release, persistedProviders),
            MenuSnapshotSource.Database,
            DateTimeOffset.UtcNow);
        if (!activation.Activated)
        {
            _lastError = string.Join(",", activation.Validation.Errors.Select(issue => issue.Code));
            return new MenuReloadResult(false, activation.Snapshot.ReleaseId, activation.Snapshot.Source,
                string.IsNullOrEmpty(_lastError) ? "release_validation_failed" : _lastError);
        }

        _lastError = null;
        _core.Scheduler.NextTick(_commandRouter.ReconcileConsoleCommands);
        await SaveLastKnownGoodSafelyAsync(release, cancellationToken).ConfigureAwait(false);
        await UpdateStatusSafelyAsync(cancellationToken).ConfigureAwait(false);
        return new MenuReloadResult(true, activation.Snapshot.ReleaseId, activation.Snapshot.Source, "release_activated");
    }

    private async Task ActivateLocalAsync(CancellationToken cancellationToken)
    {
        var result = await _bootstrap.TryActivateLocalAsync(
                LastKnownGoodPath,
                FallbackPath,
                release => _contextFactory.Create(release),
                DateTimeOffset.UtcNow,
                cancellationToken)
            .ConfigureAwait(false);
        if (!result.Activated)
        {
            _lastError = "no_valid_database_lkg_or_fallback";
            _logger.LogError("Menu.Core запущен без пользовательских меню: DB, LKG и fallback недоступны или невалидны.");
        }
        else
        {
            _lastError = null;
            _core.Scheduler.NextTick(_commandRouter.ReconcileConsoleCommands);
            if (result.Source == MenuSnapshotSource.LastKnownGood)
            {
                _lastKnownGoodReleaseId = result.Snapshot.ReleaseId;
            }
            else if (result.Source == MenuSnapshotSource.Fallback)
            {
                _fallbackReleaseId = result.Snapshot.ReleaseId;
            }
            _logger.LogInformation("Menu.Core активировал Release {ReleaseId} из {Source}.",
                result.Snapshot.ReleaseId, result.Source);
        }
    }

    private async Task SaveLastKnownGoodSafelyAsync(
        MenuReleaseDefinition release,
        CancellationToken cancellationToken)
    {
        try
        {
            var validation = await _files.SaveValidatedAsync(
                    LastKnownGoodPath,
                    release,
                    _contextFactory.Create(release),
                    cancellationToken)
                .ConfigureAwait(false);
            if (!validation.IsValid)
            {
                _logger.LogWarning("Release {ReleaseId} активен, но LKG validation перед записью завершилась ошибкой.",
                    release.ReleaseId);
            }
            else
            {
                _lastKnownGoodReleaseId = release.ReleaseId;
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            _logger.LogWarning(exception,
                "Release {ReleaseId} активен, но LKG не удалось записать.", release.ReleaseId);
        }
    }

    private async Task EnsureStatusLeaseAsync(CancellationToken cancellationToken)
    {
        if (_statusLease is not null)
        {
            return;
        }

        try
        {
            _statusLease = await _statuses.StartSessionAsync(
                    new MenuRuntimeStatusRegistration(
                        _configuration.ServerKey,
                        Guid.NewGuid(),
                        "1.0.0",
                        _capabilities.Current.SwiftlyMenuApiVersion,
                        _capabilities.Current.MenuCoreApiVersion,
                        _capabilities.Current.SchemaVersion,
                        JsonSerializer.Serialize(_capabilities.Current, MenuJson.SerializerOptions)),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogDebug(exception, "Menu.Core runtime status session пока не зарегистрирована.");
        }
    }

    private async Task UpdateStatusSafelyAsync(CancellationToken cancellationToken)
    {
        if (_statusLease is null)
        {
            return;
        }

        var current = _snapshots.Current;
        try
        {
            var updated = await _statuses.UpdateAsync(
                    _statusLease,
                    new MenuRuntimeStatusUpdate(
                        current.ReleaseId > 0 ? current.ReleaseId : null,
                        string.IsNullOrEmpty(current.Checksum) ? null : current.Checksum,
                        current.Source.ToStorageValue(),
                        _lastDatabaseSyncAt,
                        _lastKnownGoodReleaseId,
                        _fallbackReleaseId,
                        _snapshots.Status.LastAttemptSucceeded ? "valid" : "degraded",
                        _lastError),
                    cancellationToken)
                .ConfigureAwait(false);
            if (!updated)
            {
                _statusLease = null;
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogDebug(exception, "Menu.Core runtime status не обновлён.");
        }
    }

    private static string ResolveDataFile(string directory, string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        if (!string.Equals(Path.GetFileName(fileName), fileName, StringComparison.Ordinal)
            || fileName is "." or "..")
        {
            throw new ArgumentException("Snapshot filename must be a basename.", nameof(fileName));
        }

        return Path.Combine(Path.GetFullPath(directory), fileName);
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private void RecordDatabaseRejection(string code, string message)
    {
        _snapshots.RecordRejected(
            MenuSnapshotSource.Database,
            DateTimeOffset.UtcNow,
            new MenuReleaseValidationResult(
            [
                new Menu.Api.Results.MenuValidationIssue
                {
                    Severity = Menu.Api.Enums.MenuValidationSeverity.Error,
                    Code = code,
                    Message = message,
                    Path = "$",
                },
            ]));
    }

}
