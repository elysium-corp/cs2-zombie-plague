using System.Linq.Expressions;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Utils;
using Moq;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Schemas;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponHandlingApplicationTests
{
    [Fact]
    public void RecoilOverridesAreAbsoluteAndPreserveUnconfiguredModes()
    {
        var data = new Mock<CCSWeaponBaseVData>(MockBehavior.Strict);
        var magnitude = Modes(data, value => value.RecoilMagnitude, 30f, 25f);
        var variance = Modes(data, value => value.RecoilMagnitudeVariance, 10f, 5f);
        var angle = Modes(data, value => value.RecoilAngle, 0f, 0f);
        var angleVariance = Modes(data, value => value.RecoilAngleVariance, 70f, 60f);
        var settings = new WeaponRecoil
        {
            Magnitude = [15f],
            MagnitudeVariance = [0f, 0f],
            Angle = [-10f, 10f]
        };

        data.Object.SetRecoil(settings);
        data.Object.SetRecoil(settings);

        Assert.Equal([15f, 25f], magnitude);
        Assert.Equal([0f, 0f], variance);
        Assert.Equal([-10f, 10f], angle);
        Assert.Equal([70f, 60f], angleVariance);
    }

    [Fact]
    public void ZeroAccuracyOverridesEveryConfiguredComponentInBothModes()
    {
        var data = new Mock<CCSWeaponBaseVData>(MockBehavior.Strict);
        float[][] values =
        [
            Modes(data, value => value.Spread),
            Modes(data, value => value.InaccuracyStand),
            Modes(data, value => value.InaccuracyCrouch),
            Modes(data, value => value.InaccuracyMove),
            Modes(data, value => value.InaccuracyFire),
            Modes(data, value => value.InaccuracyJump),
            Modes(data, value => value.InaccuracyLand),
            Modes(data, value => value.InaccuracyLadder)
        ];
        data.Object.SetAccuracy(new WeaponAccuracy
        {
            Spread = [0f, 0f],
            InaccuracyStand = [0f, 0f],
            InaccuracyCrouch = [0f, 0f],
            InaccuracyMove = [0f, 0f],
            InaccuracyFire = [0f, 0f],
            InaccuracyJump = [0f, 0f],
            InaccuracyLand = [0f, 0f],
            InaccuracyLadder = [0f, 0f]
        });

        Assert.All(values, modes => Assert.Equal([0f, 0f], modes));
    }

    [Fact]
    public void MissingSettingsDoNotAccessEngineFields()
    {
        var data = new Mock<CCSWeaponBaseVData>(MockBehavior.Strict);
        data.Object.SetRecoil(null);
        data.Object.SetAccuracy(null);
        data.VerifyNoOtherCalls();
    }

    private static float[] Modes(
        Mock<CCSWeaponBaseVData> data,
        Expression<Func<CCSWeaponBaseVData, CFiringModeFloat>> property,
        float primary = 0.1f,
        float secondary = 0.2f)
    {
        var values = new[] { primary, secondary };
        var modes = new Mock<CFiringModeFloat>(MockBehavior.Strict);
        modes.Setup(value => value.Values).Returns(new ModeArray(values));
        data.Setup(property).Returns(modes.Object);
        return values;
    }

    private sealed class ModeArray(float[] values) : ISchemaFixedArray<float>
    {
        public bool IsValid => true;
        public nint Address => 0;
        public void DangerouslySetAddress(nint address) => throw new NotSupportedException();
        public int ElementAlignment => sizeof(float);
        public int ElementCount => values.Length;
        public int ElementSize => sizeof(float);
        public ref float this[int index] => ref values[index];
    }
}
