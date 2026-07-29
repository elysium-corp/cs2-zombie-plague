using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Blind(ISwiftlyCore core, BlindConfig config) : BasePassiveAbility(core)
{
    public override float Cooldown => config.CooldownTime;

    private Guid _abilityCallbackGuid = Guid.Empty;

    public override void Hook()
    {
        if (IsHooked)
        {
            return;
        }

        base.Hook();
        _abilityCallbackGuid = core.GameEvent.HookPost<EventPlayerHurt>(OnPlayerHurtPost);
    }

    public override void UnHook()
    {
        if (_abilityCallbackGuid != Guid.Empty)
        {
            core.GameEvent.Unhook(_abilityCallbackGuid);
            _abilityCallbackGuid = Guid.Empty;
        }

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
        
        if (attacker == null || !attacker.IsValid || !attacker.IsAlive || attacker.IsOnZombieTeam())
        {
            return HookResult.Continue;
        }

        if (victim == null || !victim.IsValid || !victim.IsAlive || !victim.IsOnZombieTeam())
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
