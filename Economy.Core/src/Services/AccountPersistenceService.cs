using Microsoft.Extensions.Options;
using Economy.Core.Data.Configs;
using Economy.Core.Data.Repository;

namespace Economy.Core.Services;

internal sealed class AccountPersistenceService(IAccountRepository accountRepository, IOptions<EconomyConfig> config) : IAccountPersistenceService
{
    public int LoadOrCreateBalance(long steamId, int initialBalance = -1)
    {
        if (initialBalance < 0)
        {
            initialBalance = config.Value.StartMoney;
        }

        var account = accountRepository.FindBySteamId(steamId) ?? accountRepository.Create(steamId, initialBalance);

        return account.Balance;
    }

    public void SaveBalance(long steamId, int balance)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(balance);

        if (!accountRepository.UpdateBalance(steamId, balance))
        {
            throw new InvalidOperationException($"Account for SteamID {steamId} was not found.");
        }
    }
}