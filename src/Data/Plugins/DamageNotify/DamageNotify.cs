using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data.Extensions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace CS2ZombiePlague.Data.Plugins.DamageNotify;

public class DamageNotify(ISwiftlyCore core, IOptions<ZombiePlagueCoreConfig> config)
{
    public void Start()
    {
        core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
    }

    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {

        var player = @event.AttackerPlayer;
        var victim = @event.UserIdPlayer;

        if (player == null || victim == null || !player.IsValid || !victim.IsValid)
            return HookResult.Continue;

        if (player.IsInfected() || victim.Controller.Team == player.Controller.Team)
        {
            return HookResult.Continue;
        }

        if (player.IsFakeClient)
            return HookResult.Continue;

        var victimName = victim.Controller.PlayerName;
        var localizer = core.Translation.GetPlayerLocalizer(player);

        player.SendCenterHTML(
            $"<font color='#FFFFFF'>{localizer["DamageNotify.HitMessage"]} </font>" +
            $"<font color='#FF3333'>{victimName}</font><br>" +
            $"<font color='#CCFF00'>{victim.RequiredPlayerPawn.Health}</font>" +
            $" <font color='#FFFFFF'></font> " +
            $"<font color='#FF3333'>-{@event.DmgHealth}</font>"
            , config.Value.DamageNotifyDuration);

        return HookResult.Continue;
    }
}