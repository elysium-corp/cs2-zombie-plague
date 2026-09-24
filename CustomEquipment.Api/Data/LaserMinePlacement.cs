using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;

namespace CustomEquipment.Api.Data;

/// <summary>Проверяет поверхность установки, исключая игрока и переносимые им предметы.</summary>
public static class LaserMinePlacement
{
    /// <summary>
    /// Находит точку перед прицелом в пределах дальности установки.
    /// Вызывается в игровом потоке до создания модели мины.
    /// </summary>
    public static bool TryFindSurface(
        ISwiftlyCore core,
        CCSPlayerPawn? pawn,
        float maxDistance,
        out Vector position,
        out QAngle rotation)
    {
        position = default;
        rotation = default;
        if (pawn is not { IsValid: true, EyePosition: { } start } ||
            !float.IsFinite(maxDistance) || maxDistance <= 0f) return false;

        var trace = core.Trace.TraceShapeAngle(start, pawn.EyeAngles, maxDistance + 1f, new TraceParams
        {
            ObjectQuery = RnQueryObjectSet.All,
            InteractWith = MaskTrace.Solid | MaskTrace.WorldGeometry | MaskTrace.StaticLevel | MaskTrace.Window,
            InteractExclude = MaskTrace.Player | MaskTrace.CarriedObject | MaskTrace.CarriedWeapon,
            InteractAs = MaskTrace.Empty,
            EntitiesToIgnore = [pawn],
            OwnersToIgnore = [pawn],
            ShouldHitEntity = entity => entity is not CBasePlayerPawn and not CBasePlayerWeapon
        });

        var distance = start.Distance(trace.EndPos);
        var normalLength = trace.HitNormal.Length();
        if (!trace.DidHit || trace.StartInSolid || !float.IsFinite(distance) || distance > maxDistance ||
            !float.IsFinite(normalLength) || normalLength < 0.5f)
        {
            core.Logger.LogInformation(
                "[LaserMine] Поверхность не найдена: player={Player}, hit={Hit}, startSolid={StartSolid}, " +
                "distance={Distance}, maxDistance={MaxDistance}, normalLength={NormalLength}, entity={Entity}.",
                pawn.Index, trace.DidHit, trace.StartInSolid, distance, maxDistance, normalLength,
                trace.Entity?.DesignerName ?? "world");
            return false;
        }

        var normal = trace.HitNormal / normalLength;
        position = trace.EndPos + normal * 5f;
        rotation = normal.ToQAngles();
        return true;
    }
}
