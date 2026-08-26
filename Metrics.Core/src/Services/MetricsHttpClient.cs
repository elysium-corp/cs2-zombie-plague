using System.Net.Http.Headers;
using System.Net.Http.Json;
using Metrics.Core.Config;
using Metrics.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Metrics.Core.Services;

internal sealed class MetricsHttpClient(
    IOptions<MetricsConfig> config,
    ILogger<MetricsHttpClient> logger
) : IDisposable
{
    private const int MaxResponseBytes = 64 * 1_024;

    private readonly HttpClient _httpClient = CreateHttpClient();

    private string? _lastConfigurationError;
    private string? _lastPersistentFailureReason;
    private int _lastAuthorizationFailureStatus;

    public async Task<DeliveryOutcome> SendWithRetryAsync(
        IReadOnlyCollection<MetricEventEnvelope> events,
        CancellationToken cancellationToken
    )
    {
        if (events.Count == 0)
        {
            return DeliveryOutcome.Delivered;
        }

        var settings = config.Value;

        if (!MetricsConfigValidation.TryValidate(settings, out var configurationError))
        {
            LogConfigurationError(configurationError);

            return DeliveryOutcome.RetryLater;
        }

        if (!settings.Enabled)
        {
            return DeliveryOutcome.RetryLater;
        }

        _lastConfigurationError = null;

        SendAttemptResult result = default;
        var retryCount = Math.Clamp(settings.RetryCount, 0, 10);

        for (var attempt = 0; attempt <= retryCount; attempt++)
        {
            result = await SendOnceAsync(events, settings, cancellationToken).ConfigureAwait(false);

            if (result.Outcome == SendAttemptOutcome.Delivered)
            {
                _lastPersistentFailureReason = null;

                return DeliveryOutcome.Delivered;
            }

            if (result.Outcome == SendAttemptOutcome.Discarded)
            {
                _lastPersistentFailureReason = null;

                return DeliveryOutcome.Discarded;
            }

            if (result.Outcome == SendAttemptOutcome.Deferred || attempt == retryCount)
            {
                break;
            }

            var delay = GetRetryDelay(settings, attempt);

            logger.LogDebug(
                "Metrics batch delivery failed. Retry {RetryAttempt}/{RetryCount} in {RetryDelay}. Reason: {Reason}",
                attempt + 1,
                retryCount,
                delay,
                result.Reason
            );

            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
        }

        var failureReason = result.Reason ?? "unknown";

        if (!string.Equals(_lastPersistentFailureReason, failureReason, StringComparison.Ordinal))
        {
            _lastPersistentFailureReason = failureReason;

            logger.LogWarning(
                "Metrics batch with {EventCount} event(s) was not delivered and will be persisted. Reason: {Reason}",
                events.Count,
                failureReason
            );
        }

        return DeliveryOutcome.RetryLater;
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    private async Task<SendAttemptResult> SendOnceAsync(
        IReadOnlyCollection<MetricEventEnvelope> events,
        MetricsConfig settings,
        CancellationToken cancellationToken
    )
    {
        _ = MetricsConfigValidation.TryBuildIngestionUri(settings.BaseUrl, out var ingestionUri);

        using var request = new HttpRequestMessage(HttpMethod.Post, ingestionUri)
        {
            Content = JsonContent.Create(
                new MetricBatchRequest { Events = events },
                options: MetricsJson.Options
            )
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiSecret);

        using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        requestCancellation.CancelAfter(TimeSpan.FromSeconds(settings.RequestTimeoutSeconds));

        try
        {
            using var response = await _httpClient
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, requestCancellation.Token)
                .ConfigureAwait(false);

            if (response.IsSuccessStatusCode)
            {
                _lastAuthorizationFailureStatus = 0;

                await LogIngestionResultAsync(response, events.Count, requestCancellation.Token).ConfigureAwait(false);

                return SendAttemptResult.Delivered;
            }

            var statusCode = (int)response.StatusCode;

            if (statusCode is 401 or 403)
            {
                if (_lastAuthorizationFailureStatus != statusCode)
                {
                    _lastAuthorizationFailureStatus = statusCode;

                    logger.LogError(
                        "Elysium Metrics rejected the API credentials with HTTP {StatusCode}. Check ServerId, ApiSecret and whether the server is enabled in Flute.",
                        statusCode
                    );
                }

                return new SendAttemptResult(
                    SendAttemptOutcome.Deferred,
                    $"HTTP {statusCode} authorization failure"
                );
            }

            if (statusCode == 413)
            {
                logger.LogError(
                    "Elysium Metrics rejected a batch as too large. Reduce BatchSize or MaxEventBytes in metrics.json."
                );

                return new SendAttemptResult(
                    SendAttemptOutcome.Deferred,
                    "HTTP 413 payload too large"
                );
            }

            if (statusCode is 408 or 425 or 429 || statusCode >= 500)
            {
                return new SendAttemptResult(
                    SendAttemptOutcome.Retryable,
                    $"HTTP {statusCode}"
                );
            }

            logger.LogError(
                "Elysium Metrics permanently rejected a batch with HTTP {StatusCode}. {EventCount} event(s) will be discarded.",
                statusCode,
                events.Count
            );

            return new SendAttemptResult(
                SendAttemptOutcome.Discarded,
                $"HTTP {statusCode}"
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            return new SendAttemptResult(
                SendAttemptOutcome.Retryable,
                $"request timeout after {settings.RequestTimeoutSeconds} seconds"
            );
        }
        catch (HttpRequestException exception)
        {
            return new SendAttemptResult(
                SendAttemptOutcome.Retryable,
                exception.Message
            );
        }
    }

    private static HttpClient CreateHttpClient()
    {
        var httpClient = new HttpClient
        {
            Timeout = Timeout.InfiniteTimeSpan,
            MaxResponseContentBufferSize = MaxResponseBytes
        };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Elysium-Metrics-Core/0.1.0");

        return httpClient;
    }

    private async Task LogIngestionResultAsync(
        HttpResponseMessage response,
        int sentCount,
        CancellationToken cancellationToken
    )
    {
        if (response.Content.Headers.ContentLength is > MaxResponseBytes)
        {
            logger.LogWarning(
                "Elysium Metrics accepted a batch, but its response exceeded {MaxResponseBytes} bytes.",
                MaxResponseBytes
            );

            return;
        }

        try
        {
            await response.Content
                .LoadIntoBufferAsync(MaxResponseBytes, cancellationToken)
                .ConfigureAwait(false);

            var result = await response.Content
                .ReadFromJsonAsync<IngestionResponse>(MetricsJson.Options, cancellationToken)
                .ConfigureAwait(false);

            if (result is null)
            {
                logger.LogWarning("Elysium Metrics returned an empty success response.");

                return;
            }

            logger.LogDebug(
                "Metrics batch processed: {Accepted} accepted, {Duplicates} duplicate(s), {Rejected} rejected.",
                result.Accepted,
                result.Duplicates,
                result.Rejected
            );

            if (result.Rejected > 0)
            {
                var firstError = result.Errors.FirstOrDefault();

                logger.LogWarning(
                    "Elysium Metrics rejected {Rejected}/{Sent} event(s). First error: {Code}, field {Field}.",
                    result.Rejected,
                    sentCount,
                    firstError?.Code ?? "unknown",
                    firstError?.Field ?? "unknown"
                );
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(
                exception,
                "Elysium Metrics accepted a batch, but its response could not be parsed."
            );
        }
    }

    private static TimeSpan GetRetryDelay(MetricsConfig settings, int attempt)
    {
        var multiplier = 1L << Math.Min(attempt, 20);
        var delayMilliseconds = Math.Min(
            settings.RetryBaseDelayMilliseconds * multiplier,
            settings.RetryMaxDelaySeconds * 1_000L
        );

        return TimeSpan.FromMilliseconds(delayMilliseconds);
    }

    private void LogConfigurationError(string error)
    {
        if (string.Equals(_lastConfigurationError, error, StringComparison.Ordinal))
        {
            return;
        }

        _lastConfigurationError = error;

        logger.LogError("Metrics delivery is paused because metrics.json is invalid: {ConfigError}", error);
    }

    private enum SendAttemptOutcome
    {
        Delivered,
        Retryable,
        Deferred,
        Discarded
    }

    private readonly record struct SendAttemptResult(SendAttemptOutcome Outcome, string? Reason)
    {
        public static readonly SendAttemptResult Delivered = new(SendAttemptOutcome.Delivered, null);
    }
}
