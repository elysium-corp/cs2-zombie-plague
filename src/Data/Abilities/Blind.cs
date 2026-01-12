using CS2ZombiePlague.Config.Ability;
using CS2ZombiePlague.Data.Abilities.Contracts;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;

namespace CS2ZombiePlague.Data.Abilities;

public sealed class Blind(ISwiftlyCore core, BlindConfig config) : BasePassiveAbility(core)
{
    public override float Cooldown => config.CooldownTime;

    private Guid _abilityCallbackGuid = Guid.Empty;

    public override void Hook()
    {
        base.Hook();
        _abilityCallbackGuid = core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
    }

    public override void UnHook()
    {
        core.GameEvent.Unhook(_abilityCallbackGuid);
        base.UnHook();
    }

    public override void Use()
    {
        if (Target == null)
        {
            return;
        }
        
        core.NetMessage.SendCUserMessageFade(
            playerId: Target.PlayerID,
            duration: config.DurationEffectAfterAbilityOnAttacker,
            holdTime: config.HoldTimeEffectAfterAbilityOnAttacker,
            flags: NetMessageExt.FFadeOut,
            color: NetMessageExt.Rgba(
                r: config.RedColorEffectAfterAbilityOnAttacker,
                g: config.GreenColorEffectAfterAbilityOnAttacker,
                b: config.BlueColorEffectAfterAbilityOnAttacker,
                a: config.AlphaEffectAfterAbilityOnAttacker
            )
        );
        
        base.Use();
    }

    private HookResult OnPlayerHurtPost(EventPlayerHurt @event)
    {
        var attacker = @event.AttackerPlayer;
        var victim = @event.UserIdPlayer;
        
        if (!attacker.IsValid || !attacker.IsAlive || attacker.IsInfected())
        {
            return HookResult.Continue;
        }

        if (!victim.IsValid || !victim.IsAlive || !victim.IsInfected())
        {
            return HookResult.Continue;
        }
        
        if (!IsActive && victim.PlayerID == Caster.PlayerID)
        {
            Target = attacker;
            Use();
        }
        
        return HookResult.Continue;
    }
}