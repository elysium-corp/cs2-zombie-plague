using CS2ZombiePlague.Config;
using CS2ZombiePlague.Config.Core;
using CS2ZombiePlague.Utils;
using CS2ZombiePlague.Utils.Extensions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace CS2ZombiePlague.Data.Plugins.ScreenFade;

public class ScreenFade(ISwiftlyCore core, IOptions<ZombiePlagueCoreConfig> config)
{
    public void Start()
    {
        core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeathPost);
    }

    private HookResult OnPlayerDeathPost(EventPlayerDeath @event)
    {
        var attacker = @event.AttackerPlayer;

        if (attacker == null || !attacker.IsValid || attacker.Equals(@event.UserIdPlayer))
        {
            return HookResult.Continue;
        }
        
        core.NetMessage.SendCUserMessageFade(
            playerId: attacker.PlayerID,
            duration: config.Value.DurationScreenFade,
            holdTime: config.Value.HoldTimeScreenFade,
            flags: NetMessageExt.FFadeIn | NetMessageExt.FFadeOut,
            color: NetMessageExt.Rgba(
                r: config.Value.RedColorScreenFade,
                g: config.Value.GreenColorScreenFade,
                b: config.Value.BlueColorScreenFade,
                a: config.Value.AlphaScreenFade
            )
        );
        
        return HookResult.Continue;
    }
}