using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Core.Data.Service;

namespace ZombiePlague.Core.Tests;

public sealed class FallDamageMovementTests
{
    [Theory]
    [InlineData(DamageTypes_t.DMG_FALL, true)]
    [InlineData(DamageTypes_t.DMG_FALL | DamageTypes_t.DMG_CRUSH, true)]
    [InlineData(DamageTypes_t.DMG_BULLET, false)]
    [InlineData(DamageTypes_t.DMG_SLASH, false)]
    public void DetectsFallDamage(DamageTypes_t type, bool expected)
    {
        Assert.Equal(expected, FallDamageMovement.IsFallDamage(type));
    }

    [Fact]
    public void RestoresHorizontalVelocityChangedByFallDamage()
    {
        var before = new Vector(120f, -40f, -600f);
        var after = new Vector(185f, 30f, 0f);

        Assert.True(FallDamageMovement.TryRestore(before, after, out var velocity));
        Assert.Equal(120f, velocity.X);
        Assert.Equal(-40f, velocity.Y);
        Assert.Equal(0f, velocity.Z);
    }

    [Fact]
    public void KeepsVelocityWhenFallDamageDidNotMoveThePlayer()
    {
        var before = new Vector(120f, -40f, -600f);
        var after = new Vector(120.2f, -40.1f, 0f);

        Assert.False(FallDamageMovement.TryRestore(before, after, out var velocity));
        Assert.Equal(after.X, velocity.X);
        Assert.Equal(after.Z, velocity.Z);
    }
}
