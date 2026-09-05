using System.Numerics;
using SupplyBox.Services;
using Xunit;

namespace SupplyBox.Tests;

public sealed class FallTests
{
    [Fact]
    public void AutomaticDropPassesZeroAndLandsWithItsBottomOnTheFloor()
    {
        var fall = new SupplyBoxFall(0, 32, 160, 0, (start, end) => Floor(start, end, -24, -4));
        var z = fall.SpawnZ;
        var steps = new List<SupplyBoxFallStep>();
        for (var now = 50; now <= 1000; now += 50)
        {
            var step = fall.Step(z, now);
            steps.Add(step);
            z = step.Z;
            if (step.State != SupplyBoxFallState.Falling) break;
        }

        Assert.Contains(steps, step => step.Z == 0 && step.State == SupplyBoxFallState.Falling);
        Assert.Contains(steps, step => step.Z < 0 && step.State == SupplyBoxFallState.Falling);
        Assert.Equal(new SupplyBoxFallStep(-20, SupplyBoxFallState.Landed), steps[^1]);
    }

    [Fact]
    public void FastDropStopsOnThinRoofBeforeLowerFloor()
    {
        var fall = new SupplyBoxFall(0, 600, 1000, 0, (start, end) =>
        {
            var roof = Floor(start, end, 300, 0);
            var floor = Floor(start, end, -100, 0);
            return new(Math.Min(roof.Fraction, floor.Fraction), false);
        });

        var first = fall.Step(fall.SpawnZ, 250);
        var second = fall.Step(first.Z, 500);

        Assert.Equal(new SupplyBoxFallStep(350, SupplyBoxFallState.Falling), first);
        Assert.Equal(new SupplyBoxFallStep(300, SupplyBoxFallState.Landed), second);
    }

    [Theory]
    [InlineData(128)]
    [InlineData(-128)]
    public void ExplicitHeightPreservesLandingWithoutCallingCollisionEngine(float target)
    {
        var fall = new SupplyBoxFall(target, 60, 1000, 0, (_, _) => throw new InvalidOperationException());

        Assert.False(fall.AutomaticLanding);
        Assert.Equal(target + 60, fall.SpawnZ);
        Assert.Equal(new SupplyBoxFallStep(target, SupplyBoxFallState.Landed), fall.Step(fall.SpawnZ, 250));
    }

    [Fact]
    public void ZeroDropHeightInAutomaticModeStillFallsBelowZero()
    {
        var fall = new SupplyBoxFall(0, 0, 160, 0, (_, _) => new(1, false));

        Assert.Equal(new SupplyBoxFallStep(-8, SupplyBoxFallState.Falling), fall.Step(fall.SpawnZ, 50));
    }

    [Fact]
    public void OverlapIsRejectedInsteadOfReportingAValidLanding()
    {
        var fall = new SupplyBoxFall(0, 600, 160, 0, (_, _) => new(0, true));

        Assert.Equal(new SupplyBoxFallStep(600, SupplyBoxFallState.StartInSolid), fall.Step(fall.SpawnZ, 50));
    }

    [Fact]
    public void ContactAtTheStartOfTheSweepCanLandWithoutOverlap()
    {
        var fall = new SupplyBoxFall(0, 600, 160, 0, (_, _) => new(0, false));

        Assert.Equal(new SupplyBoxFallStep(600, SupplyBoxFallState.Landed), fall.Step(fall.SpawnZ, 50));
    }

    [Theory]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void InvalidNativeFractionCannotTeleportOrLandTheCrate(float fraction)
    {
        var fall = new SupplyBoxFall(0, 600, 160, 0, (_, _) => new(fraction, false));

        Assert.Equal(new SupplyBoxFallStep(600, SupplyBoxFallState.InvalidTrace), fall.Step(fall.SpawnZ, 50));
    }

    [Fact]
    public void EmptyColumnStopsAtWorldLimitInsteadOfFallingForever()
    {
        var fall = new SupplyBoxFall(0, 600, 160, 0, (start, end) =>
        {
            Assert.Equal(SupplyBoxFall.MinimumZ, end);
            return new(1, false);
        });

        var step = fall.Step(SupplyBoxFall.MinimumZ + 1, 50);

        Assert.Equal(new SupplyBoxFallStep(SupplyBoxFall.MinimumZ, SupplyBoxFallState.NoSurface), step);
    }

    [Fact]
    public void StalledFlightExpiresWithoutAnotherNativeCall()
    {
        var fall = new SupplyBoxFall(0, 600, 160, 0, (_, _) => throw new InvalidOperationException());

        Assert.Equal(SupplyBoxFallState.NoSurface, fall.Step(fall.SpawnZ, 300000).State);
    }

    [Fact]
    public void SlowConfiguredFallIsNotLimitedToTwoMinutes()
    {
        var fall = new SupplyBoxFall(0, 4096, 10, 0, (_, _) => new(1, false));

        Assert.Equal(SupplyBoxFallState.Falling, fall.Step(2800, 130000).State);
    }

    [Fact]
    public void LongServerFrameKeepsMovementBounded()
    {
        var fall = new SupplyBoxFall(0, 600, 160, 0, (_, _) => new(1, false));

        Assert.Equal(new SupplyBoxFallStep(560, SupplyBoxFallState.Falling), fall.Step(fall.SpawnZ, 5000));
    }

    [Fact]
    public void HullKeepsTheModelsOffsetOrigin()
    {
        var bounds = SupplyBoxFallBounds.FromModel(new(-10, -20, 0), new(30, 40, 50), 0, 0, 0);

        Assert.Equal(new(-10, -20, 0), bounds.Mins);
        Assert.Equal(new(30, 40, 50), bounds.Maxs);
    }

    [Fact]
    public void YawRotatesTheHorizontalFootprint()
    {
        var bounds = SupplyBoxFallBounds.FromModel(new(-10, -20, -30), new(10, 20, 30), 0, 90, 0);

        Near(new(-20, -10, -30), bounds.Mins);
        Near(new(20, 10, 30), bounds.Maxs);
    }

    [Fact]
    public void PitchRotatesTheBottomUsedForFloorContact()
    {
        var bounds = SupplyBoxFallBounds.FromModel(new(0, -2, 0), new(10, 2, 4), 90, 0, 0);

        Near(new(0, -2, -10), bounds.Mins);
        Near(new(4, 2, 0), bounds.Maxs);
    }

    [Fact]
    public void MissingModelBoundsAreRejectedInsteadOfUsingAnArbitraryCrateSize()
    {
        Assert.Throws<ArgumentException>(() => SupplyBoxFallBounds.FromModel(Vector3.Zero, Vector3.Zero, 0, 0, 0));
    }

    private static SupplyBoxFallHit Floor(float start, float end, float floor, float bottom)
    {
        var originAtContact = floor - bottom;
        return start >= originAtContact && end <= originAtContact
            ? new((start - originAtContact) / (start - end), false)
            : new(1, false);
    }

    private static void Near(Vector3 expected, Vector3 actual) => Assert.True(Vector3.Distance(expected, actual) < 0.0001f);
}
