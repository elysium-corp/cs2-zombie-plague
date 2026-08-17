using Common.Database.Abstractions;
using Economy.Core.Database.Entities;

namespace Economy.Core.Services;

internal sealed class AccountPersistenceService(
    ISteamEntityStore<AccountEntity> store
) : IAccountPersistenceService
{
    public async Task<int?> LoadAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var account = await store
            .FindAsync(steamId, cancellationToken)
            .ConfigureAwait(false);

        return account?.Balance;
    }

    public Task SaveAsync(ulong steamId, int balance, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(balance);

        var now = DateTime.UtcNow;

        return store.UpsertAsync(
            steamId,
            update: account =>
            {
                account.Balance = balance;
                account.UpdatedAt = now;
            },
            initialize: account =>
            {
                account.CreatedAt = now;
            },
            cancellationToken
        );
    }
}