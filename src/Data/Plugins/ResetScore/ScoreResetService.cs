using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Plugins.ResetScore;

public class ScoreResetService(ISwiftlyCore core) : IScoreResetService
{
    public void Initialize()
    {
        core.Command.RegisterCommand(
            commandName: "reset",
            handler: ResetScoreHandler,
            registerRaw: true
        );
        
        core.Command.RegisterCommandAlias("reset", "rs");
    }

    public void ResetScoreHandler(ICommandContext context)
    {
        var player = context.Sender;
        if (player == null || !player.IsValid)
        {
            return;
        }
        
        var matchStats = player.Controller.ActionTrackingServices?.MatchStats;
        if (matchStats == null)
        {
            return;
        }

        player.Controller.MVPs = 0;
        player.Controller.MVPsUpdated();
        
        ReloadMatchStats(matchStats);

        NotifyPlayer(player);
    }

    private void ReloadMatchStats(CSMatchStats_t matchStats)
    {
        matchStats.Kills = 0;
        matchStats.KillsUpdated();
        
        matchStats.Deaths = 0;
        matchStats.DeathsUpdated();
        
        matchStats.Assists = 0;
        matchStats.AssistsUpdated();
    }

    private void NotifyPlayer(IPlayer player)
    {
        var localizer = core.Translation.GetPlayerLocalizer(player);
        var message = localizer["ResetScore.ChatMessage"];
        
        switch (player.Controller.Team)
        {
            case Team.CT:
            {
                player.SendChat($"[blue][ResetScore] [green]{message}");
                break;
            }
            case Team.Spectator:
            {
                player.SendChat($"[grey][ResetScore] [green]{message}");
                break;
            }
            case Team.T:
            {
                player.SendChat($"[red][ResetScore] [green]{message}");
                break;
            }
        }
    }
}