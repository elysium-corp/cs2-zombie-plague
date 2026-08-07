using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Blind(ISwiftlyCore core, BlindConfig config) : BasePassiveAbility(core, config)
{
    public override float Cooldown => config.CooldownTime;

    private Guid _abilityCallbackGuid = Guid.Empty;

    public override void Hook()
    {
        if (!IsEnabled || _abilityCallbackGuid != Guid.Empty)
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
        if (!IsEnabled || Target == null)
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

        if (
            IsActive ||
            !Caster.IsValid ||
            victim is not { IsValid: true, IsAlive: true } ||
            victim.PlayerID != Caster.PlayerID ||
            attacker is not { IsValid: true, IsAlive: true } ||
            attacker.Controller.Team == victim.Controller.Team
        )
        {
            return HookResult.Continue;
        }

        Target = attacker;
        Use();

        return HookResult.Continue;
    }
}