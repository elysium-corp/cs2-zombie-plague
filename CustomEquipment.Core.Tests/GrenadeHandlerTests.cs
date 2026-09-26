using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Controllers;
using Moq;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class GrenadeHandlerTests
{
    [Fact]
    public void MolotovEventAppliesEffectWithoutPollingDetonationRecordedAndOnlyOnce()
    {
        var handler = new GrenadeHandler();
        var thrower = Player();
        var grenade = Mock.Of<IGrenade>();
        var position = new Vector(10, 20, 30);
        var projectile = Projectile(position);
        handler.AddThrownGrenade(thrower, projectile, grenade);
        var calls = 0;
        void Detonate(IGrenade item, CBaseCSGrenadeProjectile entity, Vector origin)
        {
            Assert.Same(grenade, item);
            Assert.Same(projectile, entity);
            Assert.Equal(position, origin);
            calls++;
        }

        handler.OnMolotovDetonated(thrower, position, Detonate);
        handler.OnMolotovDetonated(thrower, position, Detonate);
        handler.OnTick(Detonate);

        Assert.Equal(1, calls);
    }

    [Fact]
    public void MolotovEventOnlyConsumesNearestProjectileOfItsThrower()
    {
        var handler = new GrenadeHandler();
        var thrower = Player();
        var other = Player();
        var grenade = Mock.Of<IGrenade>();
        var first = Projectile(new Vector(0, 0, 0));
        var second = Projectile(new Vector(200, 0, 0));
        handler.AddThrownGrenade(thrower, first, grenade);
        handler.AddThrownGrenade(thrower, second, grenade);
        var detonated = new List<CBaseCSGrenadeProjectile>();
        void Detonate(IGrenade _, CBaseCSGrenadeProjectile entity, Vector __) => detonated.Add(entity);

        handler.OnMolotovDetonated(other, Vector.Zero, Detonate);
        handler.OnMolotovDetonated(thrower, new Vector(500, 0, 0), Detonate);
        Assert.Empty(detonated);

        handler.OnMolotovDetonated(thrower, new Vector(200, 0, 0), Detonate);
        handler.OnMolotovDetonated(thrower, Vector.Zero, Detonate);
        Assert.Equal(new[] { second, first }, detonated);
    }

    [Fact]
    public void ClearingTrackerDiscardsPreviousMapProjectiles()
    {
        var handler = new GrenadeHandler();
        var thrower = Player();
        handler.AddThrownGrenade(thrower, Projectile(Vector.Zero), Mock.Of<IGrenade>());
        handler.Clear();
        handler.OnMolotovDetonated(thrower, Vector.Zero, (_, _, _) => Assert.Fail("Старый снаряд"));
    }

    private static IPlayer Player()
    {
        var player = new Mock<IPlayer>();
        player.Setup(value => value.Equals(It.IsAny<IPlayer>()))
            .Returns((IPlayer other) => ReferenceEquals(player.Object, other));
        return player.Object;
    }

    private static CMolotovProjectile Projectile(Vector position)
    {
        var projectile = new Mock<CMolotovProjectile>(MockBehavior.Strict);
        projectile.Setup(value => value.IsValidEntity).Returns(true);
        projectile.Setup(value => value.AbsOrigin).Returns(position);
        return projectile.Object;
    }
}
