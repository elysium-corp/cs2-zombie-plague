using Common.Di;
using Common.Di.Utils;
using Microsoft.Extensions.Options;
using ScreenFade.Data.Configs;
using ScreenFade.Di;
using ScreenFade.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace ScreenFade;

[PluginMetadata(
    Id = "ScreenFade.Core", 
    Version = "0.1.0", 
    Name = "[ZP] ScreenFade",
    Author = "illusion & fdrinv",
    Description = "Faded screen by custom event")
]
internal partial class ScreenFade(ISwiftlyCore core) : Plugin<ScreenFadeModule>(core)
{
    private Guid _guidOnPlayerDeathPost = Guid.Empty; 
    
    private readonly Lazy<IOptions<ScreenFadeConfig>> _config = GetRequiredServiceLazy<IOptions<ScreenFadeConfig>>();
    
    protected override void OnLoad()
    {
        _guidOnPlayerDeathPost = core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeathPost);
    }

    protected override void OnUnload()
    {
        Core.GameEvent.Unhook(_guidOnPlayerDeathPost);
    }
    
    private HookResult OnPlayerDeathPost(EventPlayerDeath @event)
    {
        var attacker = @event.AttackerPlayer;

        if (attacker == null || !attacker.IsValid || attacker.Equals(@event.UserIdPlayer))
        {
            return HookResult.Continue;
        }

        var config = _config.Get();
        
        core.NetMessage.SendCUserMessageFade(
            playerId: attacker.PlayerID,
            duration: config.DurationMs,
            holdTime: config.HoldTimeMs,
            flags: NetMessageExt.FFadeIn | NetMessageExt.FFadeOut,
            color: NetMessageExt.Rgba(
                r: config.Red,
                g: config.Green,
                b: config.Blue,
                a: config.Alpha
            )
        );
        
        return HookResult.Continue;
    }
}