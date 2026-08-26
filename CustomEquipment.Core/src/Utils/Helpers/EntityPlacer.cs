using Common.Di;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using SwiftlyS2.Shared.Trace;

namespace CustomEquipment.Utils.Helpers;

public static class EntityPlacer
{
    public static bool CanAttachToGround(CCSPlayerPawn? playerPawn, float maxDistanceToAttach = 10000f)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();

        if (playerPawn == null || !playerPawn.IsValid) return false;

        if (playerPawn.EyePosition == null) return false;

        var start = playerPawn.EyePosition.Value;
        var forward = playerPawn.EyeAngles;

        var trace = core.Trace.TraceShapeAngle(
            start,
            forward,
            new TraceParams
            {
                ObjectQuery = RnQueryObjectSet.AllGameEntities | RnQueryObjectSet.Static,
                InteractWith = MaskTrace.Solid,
                InteractExclude = MaskTrace.Empty | MaskTrace.Player,
                InteractAs = MaskTrace.Empty,
                EntitiesToIgnore = [playerPawn]
            }
        );

        if (!trace.DidHit) return false;
        if (trace.Distance > maxDistanceToAttach) return false;

        var normal = trace.HitNormal;

        return true;
    }
}