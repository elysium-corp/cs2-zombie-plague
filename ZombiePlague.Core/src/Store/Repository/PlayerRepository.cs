using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Core.Store.Contracts;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Store.Repository;

internal class PlayerRepository(IPlayerStore playerStore) : IPlayerRepository
{
    public string GetZClassId(IPlayer player)
    {
        return GetOrCreate(player).ZClassId;
    }

    public string GetHClassId(IPlayer player)
    {
        return GetOrCreate(player).HClassId;
    }

    public string GetKnifeId(IPlayer player)
    {
        return GetOrCreate(player).KnifeId;
    }

    public void SetZClassId(IPlayer player, string classId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);

        var preferences = GetOrCreate(player);

        playerStore.Set(
            player,
            preferences with
            {
                ZClassId = classId
            }
        );
    }
    
    public void SetHClassId(IPlayer player, string classId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);

        var preferences = GetOrCreate(player);

        playerStore.Set(
            player,
            preferences with
            {
                HClassId = classId
            }
        );
    }

    public void SetKnifeId(IPlayer player, string knifeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knifeId);

        var preferences = GetOrCreate(player);

        playerStore.Set(
            player,
            preferences with
            {
                KnifeId = knifeId
            }
        );
    }

    public bool Remove(IPlayer player)
    {
        return playerStore.Remove(player);
    }

    private PlayerPreferences GetOrCreate(IPlayer player)
    {
        if (playerStore.TryGet(player, out var preferences))
        {
            return preferences;
        }

        var createdPreferences = new PlayerPreferences();

        playerStore.Set(player, createdPreferences);

        return createdPreferences;
    }
}