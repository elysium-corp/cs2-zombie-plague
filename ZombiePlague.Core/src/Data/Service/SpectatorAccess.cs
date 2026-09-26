using Admin.Api;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Config.Core;

namespace ZombiePlague.Core.Data.Service;

/// <summary>
/// Право игрока находиться в наблюдателях по разрешениям Admin.Api из <c>SpectatorPermissions</c>
/// и отметка, что он ушёл туда сам в текущем подключении на текущей карте.
/// </summary>
internal interface ISpectatorAccess
{
    /// <summary>Есть ли у игрока хотя бы одно из разрешений, открывающих наблюдателей.</summary>
    bool CanSpectate(IPlayer player);

    /// <summary>
    /// Игрок сам ушёл в наблюдатели в текущем подключении на текущей карте и по-прежнему имеет право.
    /// Только такого наблюдателя режим не возвращает в игру.
    /// </summary>
    bool IsVoluntarySpectator(IPlayer player);

    /// <summary>Отмечает, что игрок сам ушёл в наблюдатели.</summary>
    void MarkVoluntarySpectator(IPlayer player);

    /// <summary>Снимает отметку после возвращения в игру или отключения.</summary>
    void Forget(IPlayer player);

    /// <summary>Снимает все отметки при смене карты: все игроки снова входят в игру.</summary>
    void ForgetAll();
}

internal sealed class SpectatorAccess(IAdminApi admin, IOptions<ZombiePlagueCoreConfig> config) : ISpectatorAccess
{
    // Отметка привязана к подключению: после переподключения игрок входит в игру как все.
    private readonly Dictionary<int, ulong> _voluntary = [];

    public bool CanSpectate(IPlayer player)
    {
        return player.SteamID != 0 &&
               config.Value.SpectatorPermissions.Any(permission =>
                   !string.IsNullOrWhiteSpace(permission) && admin.HasPermission(player, permission));
    }

    public bool IsVoluntarySpectator(IPlayer player)
    {
        return _voluntary.TryGetValue(player.PlayerID, out var session) &&
               session == player.SessionId &&
               CanSpectate(player);
    }

    public void MarkVoluntarySpectator(IPlayer player)
    {
        _voluntary[player.PlayerID] = player.SessionId;
    }

    public void Forget(IPlayer player)
    {
        _voluntary.Remove(player.PlayerID);
    }

    public void ForgetAll()
    {
        _voluntary.Clear();
    }
}
