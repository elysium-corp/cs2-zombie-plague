using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Charge(ISwiftlyCore core, ChargeConfig config) : BaseActiveAbility(core, config)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private CancellationTokenSource? _chargeToken;
    private float? _speedBeforeCharge;

    private const uint DurationEffectAbility = 500;

    public override void Use()
    {
        var pawn = Caster.RequiredPlayerPawn;
        var startSpeed = pawn.VelocityModifier * 250f;
        var maxSpeed = config.MaxSpeed;
        var chargeTime = (float)config.ChargeTime;
        var speedUpdatePerTimeTick = config.SpeedUpdatePerTimeTick;

        _speedBeforeCharge = startSpeed;

        var holdTime = (uint)Math.Clamp(
            config.ChargeTime * 1000L - DurationEffectAbility * 2L,
            0L,
            uint.MaxValue
        );

        core.NetMessage.SendCUserMessageFade(
            playerId: Caster.PlayerID,
            duration: DurationEffectAbility,
            holdTime: holdTime,
            flags: NetMessageExt.FFadeIn | NetMessageExt.FFadeOut,
            color: NetMessageExt.Rgba(153, 40, 40, 80)
        );

        var elapsedTime = 0f;

        _chargeToken = core.Scheduler.RepeatBySeconds(speedUpdatePerTimeTick, () =>
        {
            if (!Caster.IsValid || !Caster.IsAlive)
            {
                StopCharge(restoreSpeed: false);
                return;
            }

            elapsedTime = Math.Min(elapsedTime + speedUpdatePerTimeTick, chargeTime);

            if (elapsedTime >= chargeTime)
            {
                StopCharge(restoreSpeed: true);
                return;
            }

            var progress = elapsedTime / chargeTime;
            var currentSpeed = startSpeed + (maxSpeed - startSpeed) * progress;
            Caster.SetSpeed(currentSpeed);
        });

        base.Use();
    }

    protected override bool CanUse()
    {
        return
            Caster is { IsValid: true, IsAlive: true } &&
            config.ChargeTime > 0 &&
            config.SpeedUpdatePerTimeTick > 0f;
    }

    public override void UnHook()
    {
        StopCharge(restoreSpeed: true);
        base.UnHook();
    }

    public override void PlaySound()
    {
        if (config.SoundEffectNames.Count == 0)
        {
            return;
        }

        var soundName = config.SoundEffectNames[
            Random.Shared.Next(config.SoundEffectNames.Count)
        ];

        if (string.IsNullOrWhiteSpace(soundName))
        {
            return;
        }

        using var sound = new SoundEvent(soundName);

        sound.Recipients.AddAllPlayers();
        sound.SourceEntityIndex = (int)Caster.RequiredPlayerPawn.Index;

        sound.Emit();
    }

    private void StopCharge(bool restoreSpeed)
    {
        _chargeToken?.Cancel();
        _chargeToken = null;

        if (
            restoreSpeed &&
            _speedBeforeCharge is { } speed &&
            Caster is { IsValid: true, IsAlive: true }
        )
        {
            Caster.SetSpeed(speed);
        }

        _speedBeforeCharge = null;
    }
}