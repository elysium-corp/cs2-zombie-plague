using System.Reflection;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Mines;
using CustomEquipment.Controllers;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
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

public sealed class LaserMinePlacementTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void InventoryIsConsumedOnlyAfterSuccessfulEntityCreation(bool valid)
    {
        var core = Mock.Of<ISwiftlyCore>();
        var player = Mock.Of<IPlayer>();
        var equipment = new Mock<IEquipmentService>(MockBehavior.Strict);
        if (valid) equipment.Setup(value => value.RemoveItems<LaserMine>(player)).Returns(1);
        using var controller = CreateController(core, equipment.Object);
        using var mine = new TestMine(core);
        var model = new Mock<CBaseModelEntity>();
        model.SetupGet(value => value.IsValidEntity).Returns(valid);
        typeof(LaserMineEntityBase).GetProperty(nameof(LaserMineEntityBase.LaserMine))!
            .SetValue(mine, model.Object);

        typeof(MineController).GetMethod("OnMinePlaced", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(controller, [new MinePlacedContext(player, mine)]);

        equipment.Verify(value => value.RemoveItems<LaserMine>(player), valid ? Times.Once() : Times.Never());
    }

    private static MineController CreateController(
        ISwiftlyCore core, IEquipmentService equipment, GameplayItemCatalog? catalog = null) =>
        new(core, Mock.Of<ICustomEquipmentEvents>(), equipment, Mock.Of<ILaserMineInstallerService>(),
            () => Mock.Of<IZombiePlagueApi>(), Mock.Of<ILocalizationApi>(), catalog ?? new GameplayItemCatalog());

    private sealed class TestMine(ISwiftlyCore core) : LaserMineEntityBase(core);
}
