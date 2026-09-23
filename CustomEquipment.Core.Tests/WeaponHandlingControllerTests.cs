using CustomEquipment.Controllers;
using CustomEquipment.Services;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.GameHooks;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponHandlingControllerTests
{
    [Fact]
    public void ReinitializationDoesNotLeaveCommandOrShotHooks()
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var command = Mock.Get(core.Object.GameHooks.Movement.RunCommand);
        var firstHook = Guid.NewGuid();
        var secondHook = Guid.NewGuid();
        core.SetupSequence(value => value.GameEvent.HookPre(
                It.IsAny<IGameEventService.GameEventHandler<EventWeaponFire>>()))
            .Returns(firstHook)
            .Returns(secondHook);
        using var controller = new WeaponHandlingController(core.Object, Mock.Of<IEquipmentService>());

        controller.Initialize();
        controller.Initialize();
        controller.Dispose();
        controller.Dispose();
        controller.Initialize();
        controller.Dispose();

        command.VerifyAdd(value => value.Pre += It.IsAny<OnRunCommandMovementPreDelegate>(), Times.Exactly(2));
        command.VerifyRemove(value => value.Pre -= It.IsAny<OnRunCommandMovementPreDelegate>(), Times.Exactly(2));
        command.VerifyAdd(value => value.Post += It.IsAny<OnRunCommandMovementPostDelegate>(), Times.Exactly(2));
        command.VerifyRemove(value => value.Post -= It.IsAny<OnRunCommandMovementPostDelegate>(), Times.Exactly(2));
        core.Verify(value => value.GameEvent.Unhook(firstHook), Times.Once);
        core.Verify(value => value.GameEvent.Unhook(secondHook), Times.Once);
    }
}
