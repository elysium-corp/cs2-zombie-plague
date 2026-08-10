using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class DoubleJump(
    ISwiftlyCore core,
    DoubleJumpConfig config
) : BasePassiveAbility(core, config)
{
    public override float Cooldown => 0.01f;

    private int _jumpNum = 1;

    private bool _jumpPressed;

    public override void Hook()
    {
        if (!IsEnabled) return;
        if (Caster.IsFakeClient) return;

        base.Hook();

        core.GameHooks.Movement.SetupMove.Pre += Move;
    }

    public override void UnHook()
    {
        core.GameHooks.Movement.SetupMove.Pre -= Move;

        base.UnHook();
    }

    public override void Use()
    {
        var pawn = Caster.PlayerPawn;

        if (pawn == null) return;

        var gravityScale = pawn.ActualGravityScale;
        var currentVelocity = pawn.AbsVelocity;

        var jumpVelocity = CalculateJumpVelocity(gravityScale);

        var velocity = new Vector(
            currentVelocity.X,
            currentVelocity.Y,
            jumpVelocity
        );

        pawn.Teleport(null, null, velocity);

        _jumpNum--;

        base.Use();
    }

    private void Move(ref SetupMoveMovementPreContext context)
    {
        var player = context.Params.Player;

        if (player.PlayerID != Caster.PlayerID) return;

        if (!player.IsAlive) return;

        var pawn = player.Pawn;

        if (pawn == null) return;

        var buttons = context.Params.UserCmd.ButtonState;

        var isSpacePressed =
            (buttons.ButtonPressed & GameButtonFlags.Space) != 0;

        var isSpaceChanged =
            (buttons.ButtonChanged & GameButtonFlags.Space) != 0;

        var isOnGround = pawn.GroundEntity.IsValid;

        if (isOnGround)
        {
            _jumpNum = 1;
            _jumpPressed = false;
            return;
        }

        if (!isSpaceChanged)
            return;

        if (!isSpacePressed)
        {
            _jumpPressed = false;
            return;
        }

        if (_jumpPressed) return;

        _jumpPressed = true;

        if (_jumpNum <= 0) return;
        TryUse();
    }

    private void TryUse()
    {
        if (!Caster.IsValid) return;

        if (!Caster.IsAlive) return;

        Use();
    }

    private float CalculateJumpVelocity(float gravityScale)
    {
        var gravity = 800.0f * gravityScale;

        return MathF.Sqrt(
            2.0f * gravity * config.BaseJumpUnits
        );
    }
}