using CustomHud.Api;
using Common.Di;
using Localization.Api;
using ResetScore.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ResetScore;

[PluginMetadata(
    Id = "ResetScore.Core",
    Version = "0.2.0",
    Name = "[ZP] ResetScore",
    Author = "illusion & fdrinv",
    Description = "Allows a player to reset their score"
)]
internal sealed partial class ResetScore(ISwiftlyCore core) : Plugin<ResetScoreModule>(core)
{
    private readonly Lazy<BannerNotificationClient> _notifications = GetRequiredServiceLazy<BannerNotificationClient>();

    private Guid _command;
    private ILocalizationApi _localization = null!;

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        _localization = interfaceManager.GetSharedInterface<ILocalizationApi>(
            ILocalizationApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        interfaceManager.TryGetSharedInterface<IBannerNotificationApi>(IBannerNotificationApi.SharedApiKey, out var notificationApi);
        _notifications.Value.Bind(notificationApi);
    }

    protected override void OnReady()
    {
        _command = core.Command.RegisterCommand(
            commandName: "reset",
            handler: ResetScoreHandler,
            registerRaw: true
        );
        
        core.Command.RegisterCommandAlias("reset", "rs");
    }

    protected override void OnUnload()
    {
        if (_notifications.IsValueCreated) _notifications.Value.Bind(null);
        if (_command == Guid.Empty) return;
        core.Command.UnregisterCommand(_command);
        _command = Guid.Empty;
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

        player.Controller.Score = 0;
        player.Controller.ScoreUpdated();
        
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

    private void NotifyPlayer(IPlayer player) => _notifications.Value.Publish(player, "ResetScore.ResetMessage");
}
