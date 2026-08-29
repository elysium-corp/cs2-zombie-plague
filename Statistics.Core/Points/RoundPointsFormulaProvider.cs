using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Statistics.Core.Config;

namespace Statistics.Core.Points;

internal sealed class RoundPointsFormulaProvider(
    IOptionsMonitor<StatisticsConfig> config,
    ILogger<RoundPointsFormulaProvider> logger
) : IRoundPointsFormulaProvider, IDisposable
{
    private const int MaxResponseBytes = 16 * 1_024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly Lock _lock = new();

    private readonly HttpClient _httpClient = new()
    {
        Timeout = Timeout.InfiniteTimeSpan,
        MaxResponseContentBufferSize = MaxResponseBytes
    };

    private CancellationTokenSource? _lifetime;

    private Task _refreshTask = Task.CompletedTask;

    private PointsFormula? _webFormula;

    private PointsFormula? _configFormula;

    private string? _configFormulaSource;

    public void Start()
    {
        lock (_lock)
        {
            if (_lifetime is not null)
            {
                return;
            }

            _lifetime = new CancellationTokenSource();
        }

        Refresh();
    }

    public PointsFormula CaptureFormula()
    {
        lock (_lock)
        {
            if (_webFormula is not null)
            {
                return _webFormula;
            }
        }

        return GetConfigFormula();
    }

    public void Refresh()
    {
        lock (_lock)
        {
            if (_lifetime is null)
            {
                return;
            }

            if (_refreshTask.IsCompleted)
            {
                var cancellationToken = _lifetime.Token;

                _refreshTask = Task.Run(
                    () => RefreshAsync(cancellationToken),
                    CancellationToken.None
                );
            }

        }
    }

    public void StopAndWait()
    {
        CancellationTokenSource? lifetime;
        Task refreshTask;

        lock (_lock)
        {
            lifetime = _lifetime;

            if (lifetime is null)
            {
                return;
            }

            _lifetime = null;
            refreshTask = _refreshTask;
        }

        lifetime.Cancel();

        try
        {
            if (!refreshTask.Wait(TimeSpan.FromSeconds(2)))
                logger.LogWarning("Statistics formula refresh did not stop within 2000 ms; unload will continue.");
        }
        catch (OperationCanceledException)
        {
            // Expected while the plugin is unloading.
        }
        catch (AggregateException exception) when (
            exception.InnerExceptions.All(static inner => inner is OperationCanceledException))
        {
            // Expected while the plugin is unloading.
        }
        finally
        {
            lifetime.Dispose();
        }
    }

    public void Dispose()
    {
        StopAndWait();
        _httpClient.Dispose();
    }

    private async Task RefreshAsync(CancellationToken cancellationToken)
    {
        var pointsConfig = config.CurrentValue.Points;

        if (string.IsNullOrWhiteSpace(pointsConfig.WebServiceFormulaUrl))
        {
            SetWebFormula(null);

            return;
        }

        if (!TryCreateWebServiceUri(pointsConfig.WebServiceFormulaUrl, out var formulaUri))
        {
            logger.LogWarning(
                "Statistics points formula URL '{FormulaUrl}' is invalid. The config formula will be used.",
                pointsConfig.WebServiceFormulaUrl
            );

            SetWebFormula(null);

            return;
        }

        var timeoutSeconds = Math.Clamp(pointsConfig.WebServiceTimeoutSeconds, 1, 30);

        using var requestCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken
        );

        requestCancellation.CancelAfter(TimeSpan.FromSeconds(timeoutSeconds));

        try
        {
            using var response = await _httpClient
                .GetAsync(formulaUri, HttpCompletionOption.ResponseHeadersRead, requestCancellation.Token)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();

            if (response.Content.Headers.ContentLength is > MaxResponseBytes)
            {
                throw new InvalidOperationException(
                    $"Points formula response exceeds {MaxResponseBytes} bytes."
                );
            }

            await response.Content
                .LoadIntoBufferAsync(MaxResponseBytes, requestCancellation.Token)
                .ConfigureAwait(false);

            var payload = await response.Content
                .ReadFromJsonAsync<WebFormulaResponse>(JsonOptions, requestCancellation.Token)
                .ConfigureAwait(false);

            if (string.IsNullOrWhiteSpace(payload?.Formula))
            {
                throw new InvalidOperationException(
                    "Points formula response does not contain a formula."
                );
            }

            var formula = PointsFormula.Parse(payload.Formula);

            SetWebFormula(formula);

            logger.LogInformation(
                "Statistics points formula version '{FormulaVersion}' was loaded from the Web service.",
                payload.Version ?? "unspecified"
            );
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            SetWebFormula(null);

            logger.LogWarning(
                exception,
                "Failed to load the statistics points formula from the Web service. The config formula will be used."
            );
        }
    }

    private PointsFormula GetConfigFormula()
    {
        var source = config.CurrentValue.Points.DefaultFormula;

        lock (_lock)
        {
            if (_configFormula is not null &&
                string.Equals(_configFormulaSource, source, StringComparison.Ordinal))
            {
                return _configFormula;
            }
        }

        PointsFormula formula;

        try
        {
            formula = PointsFormula.Parse(source);
        }
        catch (PointsFormulaException exception)
        {
            logger.LogError(
                exception,
                "The configured statistics points formula is invalid. The built-in formula will be used."
            );

            formula = PointsFormula.Parse(PointsConfig.BuiltInDefaultFormula);
        }

        lock (_lock)
        {
            _configFormulaSource = source;
            _configFormula = formula;

            return formula;
        }
    }

    private void SetWebFormula(PointsFormula? formula)
    {
        lock (_lock)
        {
            _webFormula = formula;
        }
    }

    private static bool TryCreateWebServiceUri(string value, out Uri formulaUri)
    {
        if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
            (string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
             string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            formulaUri = uri;

            return true;
        }

        formulaUri = null!;

        return false;
    }

    private sealed class WebFormulaResponse
    {
        public string Formula { get; init; } = string.Empty;

        public string? Version { get; init; }
    }
}
