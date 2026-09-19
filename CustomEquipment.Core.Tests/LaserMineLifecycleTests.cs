using System.Reflection;
using CustomEquipment.Api.Data;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMineLifecycleTests
{
    [Fact]
    public void Dispose_CancelsTriggerBeforeDespawningEntities()
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var mine = new Mock<CBaseModelEntity>(MockBehavior.Strict);
        var tracer = new Mock<CBeam>(MockBehavior.Strict);
        using var timer = new CancellationTokenSource();
        var order = new List<string>();
        using var registration = timer.Token.Register(() => order.Add("cancel"));

        mine.SetupGet(entity => entity.IsValidEntity).Returns(true);
        mine.Setup(entity => entity.Despawn()).Callback(() => order.Add("mine"));
        tracer.SetupGet(entity => entity.IsValidEntity).Returns(true);
        tracer.Setup(entity => entity.Despawn()).Callback(() => order.Add("tracer"));

        var entity = new TestLaserMineEntity(core.Object);
        SetProperty(entity, nameof(LaserMineEntityBase.LaserMine), mine.Object);
        SetProperty(entity, "LaserMineTracer", tracer.Object);
        SetField(entity, "_triggerTask", timer);

        entity.Dispose();

        Assert.Equal(new[] { "cancel", "tracer", "mine" }, order);
        Assert.True(timer.IsCancellationRequested);
        mine.Verify(value => value.Despawn(), Times.Once);
        tracer.Verify(value => value.Despawn(), Times.Once);

        entity.Dispose();

        mine.Verify(value => value.Despawn(), Times.Once);
        tracer.Verify(value => value.Despawn(), Times.Once);
    }

    [Fact]
    public void Dispose_DisposedTimerDoesNotPreventEntityCleanup()
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var mine = new Mock<CBaseModelEntity>(MockBehavior.Strict);
        var tracer = new Mock<CBeam>(MockBehavior.Strict);
        var timer = new CancellationTokenSource();
        timer.Dispose();

        mine.SetupGet(entity => entity.IsValidEntity).Returns(true);
        mine.Setup(entity => entity.Despawn());
        tracer.SetupGet(entity => entity.IsValidEntity).Returns(true);
        tracer.Setup(entity => entity.Despawn());

        var entity = new TestLaserMineEntity(core.Object);
        SetProperty(entity, nameof(LaserMineEntityBase.LaserMine), mine.Object);
        SetProperty(entity, "LaserMineTracer", tracer.Object);
        SetField(entity, "_triggerTask", timer);

        entity.Dispose();

        mine.Verify(value => value.Despawn(), Times.Once);
        tracer.Verify(value => value.Despawn(), Times.Once);
    }

    private sealed class TestLaserMineEntity(ISwiftlyCore core) : LaserMineEntityBase(core);

    private static void SetProperty(LaserMineEntityBase entity, string name, object value) =>
        typeof(LaserMineEntityBase)
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(entity, value);

    private static void SetField(LaserMineEntityBase entity, string name, object value) =>
        typeof(LaserMineEntityBase)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(entity, value);
}
