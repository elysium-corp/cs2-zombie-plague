using CustomEquipment.Api.Data.Models;
using CustomEquipment.Utils;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponHandlingRuntimeTests
{
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(-1, false)]
    [InlineData(2, false)]
    public void ZeroRecoilOnlyAppliesToExplicitlyConfiguredMode(int mode, bool expected)
    {
        var recoil = new WeaponRecoil { Magnitude = [0f], MagnitudeVariance = [0f, 0f] };

        Assert.Equal((expected, false), WeaponHandlingRuntime.GetOverrides(recoil, null, mode));
    }

    [Fact]
    public void MissingOrPartialSettingsDoNotEnableFullSuppression()
    {
        Assert.Equal((false, false), WeaponHandlingRuntime.GetOverrides(null, null, 0));
        Assert.Equal((false, false), WeaponHandlingRuntime.GetOverrides(new WeaponRecoil(), new WeaponAccuracy(), 0));
        Assert.Equal((false, false), WeaponHandlingRuntime.GetOverrides(
            new WeaponRecoil { Magnitude = [0f] }, new WeaponAccuracy { Spread = [0f, 0f] }, 0));
        Assert.Equal((false, false), WeaponHandlingRuntime.GetOverrides(
            new WeaponRecoil { Magnitude = [0f], MagnitudeVariance = [1f] }, null, 0));
        Assert.Equal((false, false), WeaponHandlingRuntime.GetOverrides(
            new WeaponRecoil { Magnitude = [10f], MagnitudeVariance = [0f] }, null, 0));
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void AccuracyRequiresEveryComponentToBeZeroForCurrentMode(int mode, bool expected)
    {
        var accuracy = new WeaponAccuracy
        {
            Spread = [0f, 0f],
            InaccuracyStand = [0f, 0f],
            InaccuracyCrouch = [0f, 0f],
            InaccuracyMove = [0f, 0f],
            InaccuracyFire = [0f, 0.1f],
            InaccuracyJump = [0f, 0f],
            InaccuracyLand = [0f, 0f],
            InaccuracyLadder = [0f, 0f],
            InaccuracyJumpInitial = 0f,
            InaccuracyJumpApex = 0f,
            InaccuracyReload = 0f
        };

        Assert.Equal((false, expected), WeaponHandlingRuntime.GetOverrides(null, accuracy, mode));
    }

    [Fact]
    public void RepeatedShotsClearAccumulatedAngleVelocityAndInterpolationButPreserveDamagePunch()
    {
        var aimPunch = new AimPunchStub();
        aimPunch.UnpredictableBaseAngle = new QAngle(2f, 3f, 4f);
        aimPunch.UnpredictableBaseTick.Value = 90;

        for (var shot = 0; shot < 30; shot++)
        {
            aimPunch.PredictableBaseTick.Value = 100 + shot;
            aimPunch.PredictableBaseTickInterpAmount = 0.5f;
            aimPunch.PredictableBaseAngle = new QAngle(4f, 2f, 0f);
            aimPunch.PredictableBaseAngleVel = new QAngle(20f, 10f, 0f);

            WeaponHandlingRuntime.ResetRecoil(aimPunch);

            Assert.Equal(-1, aimPunch.PredictableBaseTick.Value);
            Assert.Equal(0f, aimPunch.PredictableBaseTickInterpAmount);
            Assert.Equal(QAngle.Zero, aimPunch.PredictableBaseAngle);
            Assert.Equal(QAngle.Zero, aimPunch.PredictableBaseAngleVel);
            Assert.Equal(new QAngle(2f, 3f, 4f), aimPunch.UnpredictableBaseAngle);
            Assert.Equal(90, aimPunch.UnpredictableBaseTick.Value);
        }

        Assert.Equal(30, aimPunch.TickUpdates);
        Assert.Equal(30, aimPunch.InterpolationUpdates);
        Assert.Equal(30, aimPunch.AngleUpdates);
        Assert.Equal(30, aimPunch.VelocityUpdates);

        WeaponHandlingRuntime.ResetRecoil(aimPunch);
        Assert.Equal(30, aimPunch.AngleUpdates);
    }

    private sealed class TickStub : GameTick_t
    {
        private int _value;
        public ref int Value => ref _value;
        public bool IsValid => true;
        public nint Address => 0;
        public void DangerouslySetAddress(nint address) => throw new NotSupportedException();
    }

    private sealed class AimPunchStub : CCSPlayer_AimPunchServices
    {
        private float _interpolation;
        private QAngle _angle;
        private QAngle _velocity;
        private QAngle _damageAngle;
        public GameTick_t PredictableBaseTick { get; } = new TickStub();
        public GameTick_t UnpredictableBaseTick { get; } = new TickStub();
        public ref float PredictableBaseTickInterpAmount => ref _interpolation;
        public ref QAngle PredictableBaseAngle => ref _angle;
        public ref QAngle PredictableBaseAngleVel => ref _velocity;
        public ref QAngle UnpredictableBaseAngle => ref _damageAngle;
        public ref CNetworkVarChainer __m_pChainEntity => throw new NotSupportedException();
        public CAnimGraphControllerPtr ComponentGraphController => throw new NotSupportedException();
        public CBasePlayerPawn Pawn => throw new NotSupportedException();
        public IPlayer? ToPlayer() => throw new NotSupportedException();
        public bool IsValid => true;
        public nint Address => 0;
        public void DangerouslySetAddress(nint address) => throw new NotSupportedException();
        public int TickUpdates { get; private set; }
        public int InterpolationUpdates { get; private set; }
        public int AngleUpdates { get; private set; }
        public int VelocityUpdates { get; private set; }
        public void PredictableBaseTickUpdated() => TickUpdates++;
        public void PredictableBaseTickInterpAmountUpdated() => InterpolationUpdates++;
        public void PredictableBaseAngleUpdated() => AngleUpdates++;
        public void PredictableBaseAngleVelUpdated() => VelocityUpdates++;
        public void UnpredictableBaseTickUpdated() => throw new NotSupportedException();
        public void UnpredictableBaseAngleUpdated() => throw new NotSupportedException();
    }
}
