using Economy.Core.Data.Rules;

namespace Economy.Core.Services;

internal interface IEconomyRulesProvider
{
    EconomyRulesSnapshot Current { get; }

    bool InitializeFromDatabase();

    Task<bool> ReloadFromDatabaseAsync(CancellationToken cancellationToken = default);
}
