using Common.Database.Abstractions;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Database.Entities;

namespace CustomKnife.Data.Services;

internal sealed class PlayerKnifePersistenceService(ISteamEntityStore<PlayerKnifeEntity> store) : IPlayerKnifePersistenceService
{
    public async Task<string?> LoadAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var entity = await store
            .FindAsync(steamId, cancellationToken)
            .ConfigureAwait(false);

        return entity?.KnifeId;
    }

    public Task SaveAsync(ulong steamId, string knifeId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knifeId);

        return store.UpsertAsync(
            steamId,
            entity =>
            {
                entity.KnifeId = knifeId;
                entity.UpdatedAtUtc = DateTime.UtcNow;
            },
            cancellationToken: cancellationToken
        );
    }
}