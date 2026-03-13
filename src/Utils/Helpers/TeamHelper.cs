using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Utils.Helpers;

public static class TeamHelper
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