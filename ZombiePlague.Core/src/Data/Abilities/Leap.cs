using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Utils;

namespace ZombiePlague.Core.Data.Abilities;

internal class Leap(ISwiftlyCore core, LeapConfig config) : BaseActiveAbility(core, config)
{
    public override KeyKind? Key => null;
    public override float Cooldown => config.CooldownTime;
    public override bool IsCooldownNotify => false;

    private const float MinScale = 1.9f;

    public override void Use()
    {
        var casterPawn = Caster.RequiredPlayerPawn;
        var viewAngles = casterPawn.EyeAngles;
        var forward = MathAlgorithm.ForwardFromAngles(viewAngles);

        var leapVelocity = forward * config.LeapDistance;
        var leapScale = Math.Min(MinScale, 1 / casterPawn.GravityScale);
        leapVelocity.Z = config.LeapBoost * leapScale;

        Caster.Teleport(casterPawn.AbsOrigin, viewAngles, leapVelocity);

        base.Use();
    }

    protected override bool CanUse()
    {
        return Caster.IsValid && Caster.IsAlive;
    }

    protected override void OnRunCommandHandler(ref RunCommandMovementPreContext context)
    {
        var playerPawn = Caster.PlayerPawn;

        if (playerPawn == null || !playerPawn.IsValid) return;

        var userCmd = context.Params.UserCmd;

        if ((userCmd.ButtonState.ButtonPressed & GameButtonFlags.Space) != 0 &&
            (userCmd.ButtonState.ButtonPressed & GameButtonFlags.Ctrl) != 0 &&
            playerPawn.GroundEntity.IsValid)
        {
            base.OnRunCommandHandler(ref context);
        }
    }
}