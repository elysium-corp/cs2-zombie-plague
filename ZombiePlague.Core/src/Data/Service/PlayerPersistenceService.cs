using Common.Database.Abstractions;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Database.Entities;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Data.Service;

internal sealed class PlayerPersistenceService(ISteamEntityStore<PlayerEntity> store) : IPlayerPersistenceService
{
    public async Task<PlayerPreferences?> LoadAsync(ulong steamId)
    {
        var entity = await store
            .FindAsync(steamId)
            .ConfigureAwait(false);

        if (entity is null)
        {
            return null;
        }

        return new PlayerPreferences
        {
            ZClassId = entity.ZombieClassId,
            HClassId = entity.HumanClassId
        };
    }

    public Task SaveAsync(ulong steamId, PlayerPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        return store.UpsertAsync(
            steamId,
            entity =>
            {
                entity.ZombieClassId =
                    preferences.ZClassId;

                entity.HumanClassId =
                    preferences.HClassId;

                entity.UpdatedAtUtc =
                    DateTime.UtcNow;
            }
        );
    }
}
