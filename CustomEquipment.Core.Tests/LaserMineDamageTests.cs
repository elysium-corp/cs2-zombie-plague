using System.Reflection;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Mines;
using CustomEquipment.Controllers;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Services;
using Localization.Api;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;
using ZombiePlague.Api;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMineDamageTests
{
    [Fact]
    public void LethalHitsQueueOneDestructionOutsideTheDamageHook()
    {
        using var setup = new Setup();
        setup.Hit(100f);
        setup.Hit(200f);
        Assert.Equal(0, setup.Mine.Destructions);
        setup.Model.Verify(value => value.Despawn(), Times.Never);
        var callback = Assert.Single(setup.Pending);
        callback();
        callback();
        Assert.Equal(1, setup.Mine.Destructions);
        setup.Model.Verify(value => value.Despawn(), Times.Once);
    }

    [Fact]
    public void RoundCleanupCancelsPendingExplosion()
    {
        using var setup = new Setup();
        setup.Hit(100f);
        var callback = Assert.Single(setup.Pending);
        typeof(MineController).GetMethod("RemoveAllMines", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(setup.Controller, null);
        callback();
        Assert.Equal(0, setup.Mine.Destructions);
        setup.Model.Verify(value => value.Despawn(), Times.Once);
    }

    [Theory]
    [InlineData(-1f)]
    [InlineData(0f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void InvalidDamageDoesNotDestroyMine(float damage)
    {
        using var setup = new Setup();
        setup.Hit(damage);
        setup.Model.Verify(value => value.HealthUpdated(), Times.Never);
        Assert.Empty(setup.Pending);
    }

    [Fact]
    public void TeammateCannotDestroyMineButOwnerCan()
    {
        using var setup = new Setup();
        var teammate = new Mock<IPlayer>();
        var pawn = new Mock<CCSPlayerPawn>();
        pawn.SetupProperty(value => value.Team, Team.CT);
        teammate.SetupGet(value => value.IsValid).Returns(true);
        teammate.SetupGet(value => value.PlayerPawn).Returns(pawn.Object);
        setup.Controller.ApplyMineDamage(setup.Model.Object, teammate.Object, 100f);
        Assert.Empty(setup.Pending);
        setup.Controller.ApplyMineDamage(setup.Model.Object, setup.Owner, 100f);
        Assert.Single(setup.Pending);
    }

    private sealed class Setup : IDisposable
    {
        public Mock<CBaseModelEntity> Model { get; } = new() { DefaultValueProvider = new LaserMineSchemaDefaults() };
        public List<Action> Pending { get; } = [];
        public Mine Mine { get; }
        public IPlayer Owner { get; } = Mock.Of<IPlayer>();
        public MineController Controller { get; }

        public Setup()
        {
            var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
            core.Setup(value => value.Scheduler.NextWorldUpdate(It.IsAny<Action>())).Callback<Action>(Pending.Add);
            var events = new Mock<ICustomEquipmentEvents> { DefaultValue = DefaultValue.Mock };
            var zombiePlague = new Mock<IZombiePlagueApi> { DefaultValue = DefaultValue.Mock };
            Controller = new MineController(core.Object, events.Object, Mock.Of<IEquipmentService>(),
                Mock.Of<ILaserMineInstallerService>(), () => zombiePlague.Object,
                Mock.Of<ILocalizationApi>(), new GameplayItemCatalog());
            Controller.Initialize();
            Model.SetupGet(value => value.IsValidEntity).Returns(true);
            Model.SetupProperty(value => value.Team, Team.CT);
            Mine = new Mine(core.Object);
            typeof(LaserMineEntityBase).GetProperty(nameof(LaserMineEntityBase.LaserMine))!
                .SetValue(Mine, Model.Object);
            typeof(MineController).GetMethod("OnMinePlaced", BindingFlags.NonPublic | BindingFlags.Instance)!
                .Invoke(Controller, [new MinePlacedContext(Owner, Mine)]);
        }

        public void Hit(float damage) => Controller.ApplyMineDamage(Model.Object, null, damage);
        public void Dispose()
        {
            Controller.Dispose();
            Mine.Dispose();
        }
    }

    private sealed class Mine(ISwiftlyCore core) : LaserMineEntityBase(core)
    {
        public int Destructions { get; private set; }
        protected override void OnDestroyedByDamage() => Destructions++;
    }
}
