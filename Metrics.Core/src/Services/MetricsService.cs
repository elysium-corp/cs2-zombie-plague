using System.Collections.Concurrent;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;
using Metrics.Api;
using Metrics.Core.Config;
using Metrics.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace Metrics.Core.Services;

internal sealed class MetricsService : IMetricsService, IDisposable
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(3);
    private const int UnknownWarningCapacity = 256;
    private static readonly JsonElement EmptyProperties = JsonSerializer.SerializeToElement(
        new Dictionary<string, object?>(),
        MetricsJson.Options
    );

    private readonly ISwiftlyCore _core;
    private readonly IOptions<MetricsConfig> _config;
    private readonly MetricsHttpClient _httpClient;
    private readonly MetricsSpool _spool;
    private readonly ILogger<MetricsService> _logger;
    private readonly Channel<MetricEventEnvelope> _queue;
    private readonly ConcurrentDictionary<string, byte> _unknownEventWarnings = new(StringComparer.Ordinal);
    private readonly Lock _unknownWarningLock = new();
    private int _unknownWarningCapacityLogged;
    private readonly Lock _lifecycleLock = new();
    private readonly string _sessionId = "session_" + Guid.NewGuid().ToString("N");

    private CancellationTokenSource? _lifetime;
    private Task _worker = Task.CompletedTask;
    private string? _lastConfigurationError;
    private int _accepting;
    private long _droppedEvents;

    public MetricsService(
        ISwiftlyCore core,
        IOptions<MetricsConfig> config,
        MetricsHttpClient httpClient,
        MetricsSpool spool,
        ILogger<MetricsService> logger
    )
    {
        _core = core;
        _config = config;
        _httpClient = httpClient;
        _spool = spool;
        _logger = logger;

        var capacity = Math.Clamp(config.Value.QueueCapacity, 1, 100_000);

        _queue = Channel.CreateBounded<MetricEventEnvelope>(new BoundedChannelOptions(capacity)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.Wait,
            AllowSynchronousContinuations = false
        });
    }

    public void Start()
    {
        var settings = _config.Value;

        if (!settings.Enabled)
        {
            _logger.LogInformation(
                "Metrics.Core is loaded but disabled. Configure metrics.json and restart the plugin to enable ingestion."
            );

            return;
        }

        if (!MetricsConfigValidation.TryValidate(settings, out var error))
        {
            LogConfigurationError(error);

            return;
        }

        lock (_lifecycleLock)
        {
            if (_lifetime is not null)
            {
                return;
            }

            _lifetime = new CancellationTokenSource();
            Volatile.Write(ref _accepting, 1);
            _worker = Task.Run(() => RunWorkerAsync(_lifetime.Token), CancellationToken.None);
        }

        _logger.LogInformation(
            "Metrics.Core started for server {ServerId}. Events will be delivered to {BaseUrl} in batches of up to {BatchSize}.",
            settings.ServerId,
            settings.BaseUrl,
            settings.BatchSize
        );
    }

    public void StopAndWait()
    {
        CancellationTokenSource? lifetime;
        Task worker;

        lock (_lifecycleLock)
        {
            lifetime = _lifetime;

            if (lifetime is null)
            {
                return;
            }

            _lifetime = null;
            Volatile.Write(ref _accepting, 0);
            _queue.Writer.TryComplete();
            lifetime.Cancel();
            worker = _worker;
        }

        try
        {
            if (!worker.Wait(ShutdownTimeout))
                _logger.LogWarning("Metrics worker exceeded the {TimeoutMs} ms shutdown deadline.", ShutdownTimeout.TotalMilliseconds);
        }
        catch (OperationCanceledException)
        {
            // Ожидаемая ситуация во время выгрузки плагина.
        }
        catch (AggregateException exception) when (
            exception.InnerExceptions.All(static inner => inner is OperationCanceledException))
        {
            // Ожидаемая ситуация во время выгрузки плагина.
        }
        finally
        {
            lifetime.Dispose();
        }
    }

    public void Track(string eventName, ulong? steamId = null, object? properties = null)
    {
        if (Volatile.Read(ref _accepting) == 0)
        {
            return;
        }

        try
        {
            var settings = _config.Value;

            if (!settings.Enabled)
            {
                return;
            }

            if (!MetricsConfigValidation.TryValidate(settings, out var configurationError))
            {
                LogConfigurationError(configurationError);

                return;
            }

            _lastConfigurationError = null;

            if (!MetricsConfigValidation.IsValidEventKey(eventName))
            {
                WarnUnknownEventOnce(eventName, "event key is invalid");

                return;
            }

            if (!settings.SchemaVersions.TryGetValue(eventName, out var schemaVersion) || schemaVersion < 1)
            {
                WarnUnknownEventOnce(eventName, "schema version is not configured");

                return;
            }

            if (!TrySerializeProperties(properties, settings.MaxEventBytes, out var serializedProperties))
            {
                return;
            }

            var metricEvent = new MetricEventEnvelope
            {
                EventId = Guid.NewGuid().ToString("N"),
                EventKey = eventName,
                SchemaVersion = schemaVersion,
                OccurredAt = DateTimeOffset.UtcNow,
                ServerId = settings.ServerId,
                SteamId = steamId is > 0
                    ? steamId.Value.ToString(CultureInfo.InvariantCulture)
                    : null,
                SessionId = settings.IncludeSessionId ? _sessionId : null,
                Map = settings.IncludeMap ? CaptureMap() : null,
                ReleaseVersion = NormalizeContextValue(settings.ReleaseVersion),
                Properties = serializedProperties
            };

            if (_queue.Writer.TryWrite(metricEvent))
            {
                return;
            }

            var droppedCount = Interlocked.Increment(ref _droppedEvents);

            if (droppedCount == 1 || droppedCount % 100 == 0)
            {
                _logger.LogWarning(
                    "Metrics in-memory queue is full. {DroppedEventCount} event(s) have been dropped since startup.",
                    droppedCount
                );
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(
                exception,
                "Metrics event '{EventKey}' could not be queued.",
                eventName
            );
        }
    }

    public void Dispose()
    {
        StopAndWait();
    }

    private async Task RunWorkerAsync(CancellationToken cancellationToken)
    {
        var activeBatch = new List<MetricEventEnvelope>(Math.Clamp(_config.Value.BatchSize, 1, 1_000));

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await ReplayOneSpoolBatchAsync(cancellationToken).ConfigureAwait(false);
                    await CollectLiveBatchAsync(activeBatch, cancellationToken).ConfigureAwait(false);

                    if (activeBatch.Count == 0)
                    {
                        continue;
                    }

                    var outcome = await _httpClient
                        .SendWithRetryAsync(activeBatch, cancellationToken)
                        .ConfigureAwait(false);

                    if (outcome == DeliveryOutcome.RetryLater)
                    {
                        await PersistSafelyAsync(activeBatch).ConfigureAwait(false);
                    }

                    activeBatch.Clear();
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    await PersistSafelyAsync(activeBatch).ConfigureAwait(false);
                    activeBatch.Clear();

                    _logger.LogError(
                        exception,
                        "Unexpected error in the Metrics delivery worker. It will continue running."
                    );

                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(1), cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                    {
                        break;
                    }
                }
            }
        }
        finally
        {
            while (_queue.Reader.TryRead(out var metricEvent))
            {
                activeBatch.Add(metricEvent);
            }

            await PersistSafelyAsync(activeBatch).ConfigureAwait(false);
        }
    }

    private async Task ReplayOneSpoolBatchAsync(CancellationToken cancellationToken)
    {
        var settings = _config.Value;

        if (!settings.Enabled ||
            !settings.PersistentSpoolEnabled ||
            !MetricsConfigValidation.TryValidate(settings, out _))
        {
            return;
        }

        var spoolBatch = await _spool
            .ReadBatchAsync(settings.BatchSize, cancellationToken)
            .ConfigureAwait(false);

        if (spoolBatch.NextOffset <= spoolBatch.StartOffset)
        {
            return;
        }

        if (spoolBatch.Events.Count == 0)
        {
            await _spool
                .AcknowledgeAsync(spoolBatch, cancellationToken)
                .ConfigureAwait(false);

            return;
        }

        var outcome = await _httpClient
            .SendWithRetryAsync(spoolBatch.Events, cancellationToken)
            .ConfigureAwait(false);

        if (outcome is DeliveryOutcome.Delivered or DeliveryOutcome.Discarded)
        {
            await _spool
                .AcknowledgeAsync(spoolBatch, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private async Task CollectLiveBatchAsync(
        List<MetricEventEnvelope> batch,
        CancellationToken cancellationToken
    )
    {
        batch.Clear();

        var settings = _config.Value;
        var flushInterval = TimeSpan.FromMilliseconds(settings.FlushIntervalMilliseconds);

        if (!await WaitForDataAsync(flushInterval, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var deadline = DateTimeOffset.UtcNow + flushInterval;

        while (batch.Count < settings.BatchSize)
        {
            while (batch.Count < settings.BatchSize && _queue.Reader.TryRead(out var metricEvent))
            {
                batch.Add(metricEvent);
            }

            if (batch.Count >= settings.BatchSize)
            {
                return;
            }

            var remaining = deadline - DateTimeOffset.UtcNow;

            if (remaining <= TimeSpan.Zero ||
                !await WaitForDataAsync(remaining, cancellationToken).ConfigureAwait(false))
            {
                return;
            }
        }
    }

    private async Task<bool> WaitForDataAsync(TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCancellation.CancelAfter(timeout);

        try
        {
            return await _queue.Reader
                .WaitToReadAsync(timeoutCancellation.Token)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return false;
        }
    }

    private async Task PersistSafelyAsync(IReadOnlyCollection<MetricEventEnvelope> events)
    {
        if (events.Count == 0)
        {
            return;
        }

        if (!_config.Value.PersistentSpoolEnabled)
        {
            _logger.LogWarning(
                "{EventCount} undelivered Metrics event(s) were dropped because persistent spool is disabled.",
                events.Count
            );

            return;
        }

        try
        {
            await _spool.AppendAsync(events, CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogError(
                exception,
                "Failed to persist {EventCount} undelivered Metrics event(s).",
                events.Count
            );
        }
    }

    private bool TrySerializeProperties(object? properties, int maxBytes, out JsonElement result)
    {
        try
        {
            result = properties switch
            {
                null => EmptyProperties,
                JsonElement element => element.Clone(),
                _ => JsonSerializer.SerializeToElement(properties, MetricsJson.Options)
            };

            if (result.ValueKind != JsonValueKind.Object)
            {
                _logger.LogWarning("Metrics event properties must serialize to a JSON object.");

                return false;
            }

            var payloadBytes = Encoding.UTF8.GetByteCount(result.GetRawText());

            if (payloadBytes > maxBytes)
            {
                _logger.LogWarning(
                    "Metrics event properties contain {PayloadBytes} bytes and exceed the configured {MaxEventBytes} byte limit.",
                    payloadBytes,
                    maxBytes
                );

                return false;
            }

            return true;
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException)
        {
            _logger.LogWarning(exception, "Metrics event properties could not be serialized.");

            result = default;

            return false;
        }
    }

    private string? CaptureMap()
    {
        try
        {
            if (!_core.IsGameThread)
            {
                return null;
            }

            return NormalizeContextValue(_core.Engine.GlobalVars.MapName.Value);
        }
        catch
        {
            return null;
        }
    }

    private void WarnUnknownEventOnce(string eventName, string reason)
    {
        var warningKey = eventName + ":" + reason;

        lock (_unknownWarningLock)
        {
            if (_unknownEventWarnings.ContainsKey(warningKey)) return;
            if (_unknownEventWarnings.Count >= UnknownWarningCapacity)
            {
                if (Interlocked.Exchange(ref _unknownWarningCapacityLogged, 1) == 0)
                    _logger.LogWarning("Metrics unknown-event warning cache reached its {Capacity} key limit.", UnknownWarningCapacity);
                return;
            }

            _unknownEventWarnings.TryAdd(warningKey, 0);
        }

        _logger.LogWarning("Metrics event '{EventKey}' was ignored because its {Reason}.", eventName, reason);
    }

    private void LogConfigurationError(string error)
    {
        if (string.Equals(_lastConfigurationError, error, StringComparison.Ordinal))
        {
            return;
        }

        _lastConfigurationError = error;

        _logger.LogError("Metrics event collection is disabled because metrics.json is invalid: {ConfigError}", error);
    }

    private static string? NormalizeContextValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmedValue = value.Trim();

        return trimmedValue.Length <= 128
            ? trimmedValue
            : trimmedValue[..128];
    }
}
