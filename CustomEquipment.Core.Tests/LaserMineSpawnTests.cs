using CustomEquipment.Api.Data;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMineSpawnTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ModelIsSetOnlyAfterSuccessfulDispatchSpawn(bool deletedDuringSpawn)
    {
        var core = new Mock<ISwiftlyCore> { DefaultValue = DefaultValue.Mock };
        core.SetupGet(value => value.Logger).Returns(NullLogger.Instance);
        var model = new Mock<CBaseModelEntity> { DefaultValueProvider = new LaserMineSchemaDefaults() };
        var pawn = new Mock<CCSPlayerPawn> { DefaultValueProvider = new LaserMineSchemaDefaults() };
        pawn.SetupGet(value => value.IsValid).Returns(true);
        pawn.SetupGet(value => value.EyePosition).Returns(new Vector(0, 0, 64));
        var player = new Mock<IPlayer>();
        player.SetupGet(value => value.IsValid).Returns(true);
        player.SetupGet(value => value.IsAlive).Returns(true);
        player.SetupGet(value => value.PlayerPawn).Returns(pawn.Object);
        var spawned = false;
        model.SetupGet(value => value.IsValidEntity).Returns(() => !spawned || !deletedDuringSpawn);
        model.Setup(value => value.DispatchSpawn(null)).Callback(() => spawned = true);
        model.Setup(value => value.SetModel(It.IsAny<string>()))
            .Callback(() => Assert.True(spawned))
            .Throws<ModelBoundaryException>();
        core.Setup(value => value.EntitySystem.CreateEntityByDesignerName<CBaseModelEntity>("prop_dynamic_override"))
            .Returns(model.Object);
        object trace = new TraceResult();
        foreach (var (name, value) in new (string, object)[]
                 {
                     (nameof(TraceResult.EndPos), new Vector(50, 0, 64)),
                     (nameof(TraceResult.HitNormal), new Vector(-1, 0, 0)),
                     (nameof(TraceResult.Fraction), 0.5f)
                 })
        {
            typeof(TraceResult).GetProperty(name)!.SetValue(trace, value);
        }
        core.Setup(value => value.Trace.TraceShapeAngle(
                It.Ref<Vector>.IsAny, It.Ref<QAngle>.IsAny, It.IsAny<float>(), It.Ref<TraceParams?>.IsAny))
            .Returns((TraceResult)trace);
        using var timer = new CancellationTokenSource();
        core.Setup(value => value.Scheduler.DelayBySeconds(It.IsAny<float>(), It.IsAny<Action>())).Returns(timer);
        using var mine = new Mine(core.Object);

        if (deletedDuringSpawn)
        {
            Assert.False(mine.TrySpawn(player.Object, 100f));
        }
        else
        {
            // Останавливаем тест на native-границе SetModel: schema ref-поля требует движок.
            Assert.Throws<ModelBoundaryException>(() => mine.TrySpawn(player.Object, 100f));
        }
        model.Verify(value => value.DispatchSpawn(null), Times.Once);
        model.Verify(value => value.SetModel(mine.LaserMineModel), deletedDuringSpawn ? Times.Never() : Times.Once());
        Assert.False(mine.Spawned);
    }

    private sealed class ModelBoundaryException : Exception;

    private sealed class Mine(ISwiftlyCore core) : LaserMineEntityBase(core)
    {
        public override float ArmingDelay => 1f;
        public bool Spawned { get; private set; }
        protected override void OnSpawned() => Spawned = true;
    }
}

internal sealed class LaserMineSchemaDefaults : DefaultValueProvider
{
    protected override object GetDefaultValue(Type type, Mock mock)
    {
        // Schema-поля возвращают ref: тестам нужен адрес значения, а не null.
        if (type.IsByRef) type = type.GetElementType()!;
        if (type.IsValueType) return Activator.CreateInstance(type)!;
        if (!type.IsInterface) return null!;
        var nested = (Mock)Activator.CreateInstance(typeof(Mock<>).MakeGenericType(type))!;
        nested.DefaultValueProvider = this;
        return nested.Object;
    }
}
