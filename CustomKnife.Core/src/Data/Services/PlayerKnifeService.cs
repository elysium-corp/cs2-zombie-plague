using CustomKnife.Data.Knives;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Data.Store;

namespace CustomKnife.Data.Services;

internal sealed class PlayerKnifeService(
    PlayerKnifeStore store,
    IPlayerKnifePersistenceService persistenceService
) : IPlayerKnifeService
{
    public async Task InitializeAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var preferences = store.GetOrCreate(steamId, KnifeDefaults.DefaultKnifeId);

        var databaseKnifeId = await persistenceService
            .LoadAsync(steamId, cancellationToken)
            .ConfigureAwait(false);

        if (databaseKnifeId is null)
        {
            return;
        }

        store.TrySetKnifeId(steamId, preferences, databaseKnifeId);
    }

    public string? GetKnifeId(ulong steamId)
    {
        return store
            .Get(steamId)?
            .KnifeId;
    }

    public async Task SetKnifeIdAsync(ulong steamId, string knifeId, CancellationToken cancellationToken = default)
    {
        store.SetKnifeId(steamId, knifeId);

        await persistenceService
            .SaveAsync(steamId, knifeId, cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task RemoveAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var preferences = store.Get(steamId);

        if (preferences is null)
        {
            return;
        }

        try
        {
            await persistenceService
                .SaveAsync(steamId, preferences.KnifeId, cancellationToken)
                .ConfigureAwait(false);
        }
        finally
        {
            store.Remove(steamId);
        }
    }
}