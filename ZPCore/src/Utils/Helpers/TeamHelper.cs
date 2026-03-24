using ZPCore.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Utils.Helpers;

internal static class TeamHelper
{
    public static void MoveAllPlayersToTeam(Team team)
    {
        var core = DependencyManager.GetService<ISwiftlyCore>();
        var players = core.PlayerManager.GetAllValidPlayers();
        
        foreach (var player in players)
        {
            player.SwitchTeam(team);
        }
    }
}