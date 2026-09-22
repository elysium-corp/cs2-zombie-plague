using CustomEquipment.Controllers;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.NetMessages;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponSoundControllerTests
{
    [Theory]
    [InlineData(WeaponSound_t.WEAPON_SOUND_EMPTY)]
    [InlineData(WeaponSound_t.WEAPON_SOUND_SECONDARY_EMPTY)]
    public void EmptyAttackDoesNotResolveOrEmitCustomFireSound(WeaponSound_t soundType)
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var equipment = new Mock<IEquipmentService>(MockBehavior.Strict);
        INetMessageService.ServerNetMessageHandler<CMsgTEFireBullets>? handler = null;
        core.Setup(value => value.NetMessage.HookServerMessage(
                It.IsAny<INetMessageService.ServerNetMessageHandler<CMsgTEFireBullets>>()))
            .Callback<INetMessageService.ServerNetMessageHandler<CMsgTEFireBullets>>(value => handler = value)
            .Returns(Guid.NewGuid());

        using var controller = new WeaponSoundController(core.Object, equipment.Object, Mock.Of<IItemRegistry>());
        controller.Initialize();
        var message = new Mock<CMsgTEFireBullets>(MockBehavior.Strict);
        message.SetupGet(value => value.SoundType).Returns((int)soundType);

        Assert.NotNull(handler);
        Assert.Equal(HookResult.Continue, handler(message.Object));
        equipment.VerifyNoOtherCalls();
        message.VerifyGet(value => value.SoundType, Times.Once);
        message.VerifyNoOtherCalls();
    }

    [Fact]
    public void ReinitializationDoesNotLeaveDuplicateFireMessageHooks()
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var firstHook = Guid.NewGuid();
        var secondHook = Guid.NewGuid();
        core.SetupSequence(value => value.NetMessage.HookServerMessage(
                It.IsAny<INetMessageService.ServerNetMessageHandler<CMsgTEFireBullets>>()))
            .Returns(firstHook)
            .Returns(secondHook);
        using var controller = new WeaponSoundController(
            core.Object, Mock.Of<IEquipmentService>(), Mock.Of<IItemRegistry>());

        controller.Initialize();
        controller.Initialize();
        controller.Dispose();
        controller.Dispose();
        controller.Initialize();
        controller.Dispose();

        core.Verify(value => value.NetMessage.HookServerMessage(
            It.IsAny<INetMessageService.ServerNetMessageHandler<CMsgTEFireBullets>>()), Times.Exactly(2));
        core.Verify(value => value.NetMessage.Unhook(firstHook), Times.Once);
        core.Verify(value => value.NetMessage.Unhook(secondHook), Times.Once);
    }
}
