using Localization.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Sounds;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class Charge(ISwiftlyCore core, ChargeConfig config, Func<ILocalizationApi> localization)
    : BaseActiveAbility(core, config, localization)
{
    public override KeyKind? Key => KeyKind.E;

    public override float Cooldown => config.CooldownTime;

    private CancellationTokenSource? _chargeToken;
    private float? _speedBeforeCharge;
    private PlayerPawnReference? _chargePawn;

    private const uint DurationEffectAbility = 500;

    public override void Use()
    {
        StopCharge(restoreSpeed: true);
        if (!CanUse()) return;

        var pawn = Caster.RequiredPlayerPawn;
        var startSpeed = pawn.VelocityModifier * 250f;
        var maxSpeed = config.MaxSpeed;
        var chargeTime = (float)config.ChargeTime;
        var speedUpdatePerTimeTick = config.SpeedUpdatePerTimeTick;

        _speedBeforeCharge = startSpeed;
        var pawnReference = new PlayerPawnReference(Caster.SessionId, core.EntitySystem.GetRefEHandle(pawn).Raw);
        _chargePawn = pawnReference;

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

        CancellationTokenSource? token = null;
        token = core.Scheduler.RepeatBySeconds(speedUpdatePerTimeTick, () =>
        {
            if (!ReferenceEquals(_chargeToken, token)) return;

            if (!pawnReference.TryResolve(core, out var currentPawn))
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
            currentPawn.VelocityModifier = currentSpeed / 250f;
            currentPawn.VelocityModifierUpdated();
        });
        _chargeToken = token;

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
        try
        {
            StopCharge(restoreSpeed: true);
        }
        finally
        {
            base.UnHook();
        }
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
        var token = _chargeToken;
        var speed = _speedBeforeCharge;
        var pawnReference = _chargePawn;
        _chargeToken = null;
        _speedBeforeCharge = null;
        _chargePawn = null;

        try
        {
            token?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Освобождённый таймер не должен мешать снятию способности.
        }
        finally
        {
            if (restoreSpeed && speed is { } originalSpeed && pawnReference is { } reference &&
                reference.TryResolve(core, out var pawn))
            {
                pawn.VelocityModifier = originalSpeed / 250f;
                pawn.VelocityModifierUpdated();
            }
        }
    }
}
