using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Utils.Helpers;

internal static class TeamHelper
{
    public static void MoveAllPlayersToTeam(ISwiftlyCore core, Team team)
    {
        var players = core.PlayerManager.GetAllValidPlayers();
        
        foreach (var player in players)
        {
            player.SwitchTeam(team);
        }
    }
}
