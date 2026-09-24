using Common.Di;
using CustomEquipment.Api.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Utils.Helpers;

public static class EntityPlacer
{
    public static bool CanAttachToGround(CCSPlayerPawn? playerPawn, float maxDistanceToAttach = 10000f)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        return CanAttachToGround(core, playerPawn, maxDistanceToAttach);
    }

    /// <summary>Проверяет поверхность установки через переданное ядро в игровом потоке.</summary>
    public static bool CanAttachToGround(ISwiftlyCore core, CCSPlayerPawn? playerPawn, float maxDistanceToAttach = 10000f) =>
        LaserMinePlacement.TryFindSurface(core, playerPawn, maxDistanceToAttach, out _, out _);
}
