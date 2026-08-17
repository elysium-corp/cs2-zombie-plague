using Common.Database.Storages;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Store.Repository;

internal sealed class PlayerRepository(
    PlayerSessionStore<PlayerPreferences> sessions
) : IPlayerRepository
{
    public string GetZClassId(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return sessions
            .Get(player.SteamID)?
            .Read(data => data.ZClassId)
            ?? PlayerPreferences.DefaultZombieClassId;
    }

    public string GetHClassId(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return sessions
            .Get(player.SteamID)?
            .Read(data => data.HClassId)
            ?? PlayerPreferences.DefaultHumanClassId;
    }

    public string GetKnifeId(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        return sessions
            .Get(player.SteamID)?
            .Read(data => data.KnifeId)
            ?? PlayerPreferences.DefaultKnifeId;
    }

    public void SetZClassId(IPlayer player, string classId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);

        sessions
            .Get(player.SteamID)?
            .Update(data =>
            {
                data.ZClassId = classId;
            });
    }

    public void SetHClassId(IPlayer player, string classId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(classId);

        sessions
            .Get(player.SteamID)?
            .Update(data =>
            {
                data.HClassId = classId;
            });
    }

    public void SetKnifeId(IPlayer player, string knifeId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(knifeId);

        sessions
            .Get(player.SteamID)?
            .Update(data =>
            {
                data.KnifeId = knifeId;
            });
    }
}