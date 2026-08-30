using Economy.Core.Data.Configs;
using Economy.Core.Data.Rules;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Economy.Core.Services;

internal sealed class EconomyRulesProvider : IEconomyRulesProvider
{
    private readonly EconomyRulesRepository _repository;
    private readonly ILogger<EconomyRulesProvider> _logger;
    private EconomyRulesSnapshot _current;
    private bool _databaseUnavailable;

    public EconomyRulesProvider(
        IOptions<EconomyConfig> fallbackConfig,
        EconomyRulesRepository repository,
        ILogger<EconomyRulesProvider> logger)
    {
        _repository = repository;
        _logger = logger;
        _current = EconomyRulesSnapshot.FromConfig(fallbackConfig.Value);
    }

    public EconomyRulesSnapshot Current => Volatile.Read(ref _current);

    public bool InitializeFromDatabase()
    {
        try
        {
            return ReloadFromDatabaseAsync().GetAwaiter().GetResult();
        }
        catch (Exception exception)
        {
            ReportDatabaseUnavailable(exception);
            return false;
        }
    }

    public async Task<bool> ReloadFromDatabaseAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var loaded = await _repository.LoadAsync(cancellationToken).ConfigureAwait(false);

            if (loaded is null)
            {
                if (!_databaseUnavailable)
                {
                    _logger.LogWarning(
                        "Economy settings row was not found. Fallback configuration remains active."
                    );
                }

                _databaseUnavailable = true;
                return false;
            }

            var previous = Current;
            var changed = previous.Revision != loaded.Revision
                          || (_databaseUnavailable && previous.Revision == 0);

            if (changed)
            {
                Volatile.Write(ref _current, loaded);
            }

            if (_databaseUnavailable)
            {
                _logger.LogInformation(
                    "Economy database settings are available again. Revision {Revision} is active.",
                    loaded.Revision
                );
            }

            _databaseUnavailable = false;
            return changed;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            ReportDatabaseUnavailable(exception);
            return false;
        }
    }

    private void ReportDatabaseUnavailable(Exception exception)
    {
        if (!_databaseUnavailable)
        {
            _logger.LogWarning(
                exception,
                "Economy settings could not be loaded from PostgreSQL. The last valid snapshot or fallback configuration remains active."
            );
        }

        _databaseUnavailable = true;
    }
}
