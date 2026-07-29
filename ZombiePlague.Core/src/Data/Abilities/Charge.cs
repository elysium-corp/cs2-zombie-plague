using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Charge(ISwiftlyCore core, ChargeConfig config) : BaseActiveAbility(core)
{
    public override KeyKind? Key => KeyKind.E;
    
    public override float Cooldown => config.CooldownTime;
    
    private CancellationTokenSource? _chargeToken;

    private const uint DurationEffectAbility = 500;

    public override void Use()
    {
        var casterPawn = Caster.PlayerPawn;
        if (casterPawn == null)
        {
            return;
        }

        PlaySound();

        var startSpeed = casterPawn.VelocityModifier * 250f;
        var maxSpeed = config.MaxSpeed;
        var chargeTime = config.ChargeTime;
        var speedUpdatePerTimeTick = config.SpeedUpdatePerTimeTick;
        var deltaSpeed = (maxSpeed - startSpeed) / chargeTime * speedUpdatePerTimeTick;
        
        var currentTime = 0f;
        var currentSpeed = startSpeed;
        
        core.NetMessage.SendCUserMessageFade(
            playerId: Caster.PlayerID,
            duration: DurationEffectAbility,
            holdTime: (chargeTime * 1000) - (DurationEffectAbility * 2),
            flags: NetMessageExt.FFadeIn | NetMessageExt.FFadeOut,
            color: NetMessageExt.Rgba(153, 40, 40, 80)
        );
        
        _chargeToken = core.Scheduler.RepeatBySeconds(speedUpdatePerTimeTick, () =>
        {
            if (!Caster.IsValid || !Caster.IsAlive || !Caster.IsOnZombieTeam())
            {
                _chargeToken?.Cancel();
                return;
            }

            if (currentTime >= chargeTime)
            {
                core.Scheduler.NextTick(() => { Caster.SetSpeed(startSpeed); });
                _chargeToken?.Cancel();
                return;
            }

            currentSpeed += deltaSpeed;
            currentTime += speedUpdatePerTimeTick;
            Caster.SetSpeed(currentSpeed);
        });
        
        base.Use();
    }
    
    protected override bool CanUse()
    {
        if (!Caster.IsValid)
        {
            return false;
        }

        if (!Caster.IsAlive)
        {
            return false;
        }

        if (!Caster.IsOnZombieTeam())
        {
            return false;
        }

        return true;
    }

    public override void UnHook()
    {
        _chargeToken?.Cancel();
        _chargeToken = null;
        base.UnHook();
    }
    
    public override void PlaySound()
    {
        if (config.SoundEffectNames.Count == 0)
        {
            return;
        }

        var randomSound = config.SoundEffectNames[Random.Shared.Next(config.SoundEffectNames.Count)];

        using var sound = new SoundEvent(randomSound);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)Caster.RequiredPawn.Index;

        sound.Emit();
    }
}
