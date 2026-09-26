using Admin.Api;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Core;

namespace ZombiePlague.Core.Data.Service;

/// <summary>
/// Право игрока находиться в наблюдателях по разрешениям Admin.Api из <c>SpectatorPermissions</c>
/// и его выбор остаться там после смены карты или переподключения.
/// </summary>
internal interface ISpectatorAccess
{
    /// <summary>Есть ли у игрока хотя бы одно из разрешений, открывающих наблюдателей.</summary>
    bool CanSpectate(IPlayer player);

    /// <summary>Игрок сам ушёл в наблюдатели и по-прежнему имеет на это право.</summary>
    bool ChoseSpectators(IPlayer player);

    /// <summary>Запоминает, что игрок ушёл в наблюдатели.</summary>
    void RememberSpectator(IPlayer player);

    /// <summary>Забывает выбор после возвращения игрока в игру.</summary>
    void ForgetSpectator(IPlayer player);
}

internal sealed class SpectatorAccess(IAdminApi admin, IOptions<ZombiePlagueCoreConfig> config) : ISpectatorAccess
{
    // Выбор хранится до выгрузки плагина: смена карты переподключает игроков без команды.
    private readonly HashSet<ulong> _chosen = [];

    public bool CanSpectate(IPlayer player)
    {
        return player.SteamID != 0 &&
               config.Value.SpectatorPermissions.Any(permission =>
                   !string.IsNullOrWhiteSpace(permission) && admin.HasPermission(player, permission));
    }

    public bool ChoseSpectators(IPlayer player)
    {
        return _chosen.Contains(player.SteamID) && CanSpectate(player);
    }

    public void RememberSpectator(IPlayer player)
    {
        if (player.SteamID != 0)
        {
            _chosen.Add(player.SteamID);
        }
    }

    public void ForgetSpectator(IPlayer player)
    {
        _chosen.Remove(player.SteamID);
    }
}
