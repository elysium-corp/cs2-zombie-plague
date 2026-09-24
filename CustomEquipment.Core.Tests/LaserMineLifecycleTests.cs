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

    [Fact]
    public void DestroyByDamagePlaysEffectOnceWhileRoundCleanupRemainsSilent()
    {
        var core = Mock.Of<ISwiftlyCore>();
        using var destroyed = new TestLaserMineEntity(core);
        destroyed.DestroyByDamage();
        destroyed.DestroyByDamage();
        destroyed.Dispose();
        Assert.Equal(1, destroyed.Destructions);

        using var cleaned = new TestLaserMineEntity(core);
        cleaned.Dispose();
        cleaned.DestroyByDamage();
        Assert.Equal(0, cleaned.Destructions);
    }

    [Fact]
    public void DisposeCancelsArmingAndLateCallbackCannotCreateBeamOrTrigger()
    {
        var core = new Mock<ISwiftlyCore>(MockBehavior.Strict);
        using var mine = new TestLaserMineEntity(core.Object);
        using var timer = new CancellationTokenSource();
        SetField(mine, "_armingTask", timer);
        mine.Dispose();
        typeof(LaserMineEntityBase).GetMethod("Arm", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(mine, null);

        Assert.True(timer.IsCancellationRequested);
        Assert.False(mine.IsArmed);
        core.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RemovedMineCancelsPendingSoundsAndLateCallbacksStaySilent(bool destroyedByDamage)
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        var timers = new List<CancellationTokenSource>();
        var callbacks = new List<Action>();
        core.Setup(value => value.Scheduler.DelayBySeconds(It.IsAny<float>(), It.IsAny<Action>()))
            .Returns((float _, Action callback) =>
            {
                callbacks.Add(callback);
                var timer = new CancellationTokenSource();
                timers.Add(timer);
                return timer;
            });
        using var mine = new TestLaserMineEntity(core.Object);
        var model = new Mock<CBaseModelEntity>();
        model.SetupGet(value => value.IsValidEntity).Returns(true);
        SetProperty(mine, nameof(LaserMineEntityBase.LaserMine), model.Object);
        var emitted = 0;
        mine.Schedule(0.882358f, () => emitted++);
        mine.Schedule(2f, () => emitted++);
        callbacks[0]();
        Assert.Equal(1, emitted);
        if (destroyedByDamage) mine.DestroyByDamage();
        else mine.Dispose();
        foreach (var callback in callbacks) callback();

        Assert.Equal(2, timers.Count);
        Assert.All(timers, timer => Assert.True(timer.IsCancellationRequested));
        Assert.Equal(1, emitted);
        foreach (var timer in timers) timer.Dispose();
    }

    private sealed class TestLaserMineEntity(ISwiftlyCore core) : LaserMineEntityBase(core)
    {
        public int Destructions { get; private set; }
        public void Schedule(float delay, Action callback) => ScheduleWhileAlive(delay, callback);
        protected override void OnDestroyedByDamage() => Destructions++;
    }

    private static void SetProperty(LaserMineEntityBase entity, string name, object value) =>
        typeof(LaserMineEntityBase)
            .GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)!
            .SetValue(entity, value);

    private static void SetField(LaserMineEntityBase entity, string name, object value) =>
        typeof(LaserMineEntityBase)
            .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(entity, value);
}
