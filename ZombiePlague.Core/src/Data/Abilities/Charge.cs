using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Di;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Charge(ISwiftlyCore core, ChargeConfig config) : BaseActiveAbility(core)
{
    public override KeyKind? Key => KeyKind.E;
    
    public override float Cooldown => config.CooldownTime;
    
    private CancellationTokenSource _chargeToken = null!;
    private readonly ZombieManager _zombieManager = DependencyManager.GetService<ZombieManager>();

    private const uint DurationEffectAbility = 500;

    public override void Use()
    {
        PlaySound();
        
        var startSpeed = _zombieManager.GetZombie(Caster.PlayerID).ZClass.Speed;
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
            if (!Caster.IsValid || !Caster.IsAlive || !Caster.IsInfected())
            {
                _chargeToken.Cancel();
            }

            if (currentTime >= chargeTime)
            {
                core.Scheduler.NextTick(() => { Caster.SetSpeed(startSpeed); });
                _chargeToken.Cancel();
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

        if (!Caster.IsInfected())
        {
            return false;
        }

        return true;
    }
    
    public override void PlaySound()
    {
        var randomSound = config.SoundEffectNames[new Random().Next(config.SoundEffectNames.Count)];

        if (config.SoundEffectNames.Count == 0)
        {
            return;
        }

        using var sound = new SoundEvent(randomSound);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)Caster.RequiredPawn.Index;

        sound.Emit();
    }
}