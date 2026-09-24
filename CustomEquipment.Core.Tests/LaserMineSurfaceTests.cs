using CustomEquipment.Api.Data;
using CustomEquipment.Utils.Helpers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Services;
using SwiftlyS2.Shared.Trace;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMineSurfaceTests
{
    [Fact]
    public void PlacementIgnoresCarriedMineAndAcceptsWorldAtEyeDistance()
    {
        var pawn = CreatePawn();
        var trace = CreateTrace(new Vector(50, 0, 64), false);
        var core = CreateCore(trace, out var captured);

        Assert.True(LaserMinePlacement.TryFindSurface(core, pawn, 100f, out var position, out _));
        Assert.Equal(new Vector(45, 0, 64), position);
        var options = captured()!.Value;
        Assert.Contains(pawn, options.EntitiesToIgnore);
        Assert.Contains(pawn, options.OwnersToIgnore);
        Assert.True(options.InteractExclude.HasFlag(MaskTrace.CarriedWeapon));
        Assert.True(options.InteractExclude.HasFlag(MaskTrace.CarriedObject));
        Assert.True(options.InteractWith.HasFlag(MaskTrace.WorldGeometry));
        Assert.False(options.ShouldHitEntity!(Mock.Of<CBasePlayerWeapon>()));
        Assert.False(options.ShouldHitEntity(pawn));
        Assert.True(options.ShouldHitEntity(Mock.Of<CBaseModelEntity>()));

        Assert.True(EntityPlacer.CanAttachToGround(core, pawn, 100f));
    }

    [Theory]
    [InlineData(101f, false)]
    [InlineData(20f, true)]
    public void RejectsDistantSurfaceAndTraceStartingInsideWorld(float distance, bool startInSolid)
    {
        var core = CreateCore(CreateTrace(new Vector(distance, 0, 64), startInSolid), out _);
        Assert.False(LaserMinePlacement.TryFindSurface(core, CreatePawn(), 100f, out _, out _));
        Assert.False(EntityPlacer.CanAttachToGround(core, CreatePawn(), 100f));
    }

    private static CCSPlayerPawn CreatePawn()
    {
        var pawn = new Mock<CCSPlayerPawn> { DefaultValueProvider = new SchemaDefaults() };
        pawn.SetupGet(value => value.IsValid).Returns(true);
        pawn.SetupGet(value => value.EyePosition).Returns(new Vector(0, 0, 64));
        return pawn.Object;
    }

    private static ISwiftlyCore CreateCore(TraceResult result, out Func<TraceParams?> captured)
    {
        TraceParams? options = null;
        var trace = new Mock<ITraceManager>();
        trace.Setup(value => value.TraceShapeAngle(
                It.Ref<Vector>.IsAny, It.Ref<QAngle>.IsAny, It.IsAny<float>(), It.Ref<TraceParams?>.IsAny))
            .Callback(new TraceCallback((in Vector _, in QAngle _, float _, in TraceParams? parameters) => options = parameters))
            .Returns(result);
        var core = new Mock<ISwiftlyCore>();
        core.SetupGet(value => value.Trace).Returns(trace.Object);
        core.SetupGet(value => value.Logger).Returns(NullLogger.Instance);
        captured = () => options;
        return core.Object;
    }

    private delegate void TraceCallback(in Vector start, in QAngle angle, float distance, in TraceParams? parameters);

    private sealed class SchemaDefaults : DefaultValueProvider
    {
        protected override object GetDefaultValue(Type type, Mock mock)
        {
            // Schema-геттеры углов возвращают ref: обычный Moq оставляет для него null.
            if (type.IsByRef) type = type.GetElementType()!;
            return type.IsValueType ? Activator.CreateInstance(type)! : null!;
        }
    }

    private static TraceResult CreateTrace(Vector end, bool startInSolid)
    {
        object trace = new TraceResult();
        // Дальность определяется от переданных глаз, а не от заполнения StartPos движком.
        foreach (var (name, value) in new (string, object)[]
                 {
                     (nameof(TraceResult.StartPos), new Vector(10000, 10000, 10000)),
                     (nameof(TraceResult.EndPos), end),
                     (nameof(TraceResult.HitNormal), new Vector(-1, 0, 0)),
                     (nameof(TraceResult.Fraction), 0.5f),
                     (nameof(TraceResult.StartInSolid), startInSolid)
                 })
        {
            typeof(TraceResult).GetProperty(name)!.SetValue(trace, value);
        }

        return (TraceResult)trace;
    }
}
