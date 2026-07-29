using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Data.Abilities;

internal class Leap(ISwiftlyCore core, LeapConfig config) : BaseActiveAbility(core)
{
    public override KeyKind? Key => KeyKind.Ctrl;
    public override float Cooldown => config.CooldownTime;
    public override bool IsCooldownNotify => false;

    public override void Use()
    {
        var casterPawn = Caster.RequiredPlayerPawn;
        var viewAngles = casterPawn.EyeAngles;
        var forward = MathAlgorithm.ForwardFromAngles(viewAngles);

        var leapVelocity = forward * config.LeapDistance;
        var leapScale = Math.Min(1.9f, 1 / casterPawn.GravityScale);
        leapVelocity.Z = config.LeapBoost * leapScale;

        Caster.Teleport(casterPawn.AbsOrigin, viewAngles, leapVelocity);

        base.Use();
    }

    protected override bool CanUse()
    {
        if (!Caster.IsValid || !Caster.IsAlive || !Caster.IsOnZombieTeam())
        {
            return false;
        }

        if (Caster.PlayerPawn is not { } pawn || pawn.MovementServices is not { } movement)
        {
            return false;
        }
        
        if ((movement.Buttons.ButtonPressed & GameButtonFlags.Space) == 0)
        {
            return false;
        }

        return true;
    }
}
