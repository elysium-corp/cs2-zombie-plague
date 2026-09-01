using Common.Di;
using Common.Math;
using CustomEquipment.Data.GameplayItems;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Weapons.Grenades;

/// <summary>
/// Представляет прыжковую гранату с параметрами из PostgreSQL-каталога.
/// </summary>
public sealed class JumpNade : ManagedGrenadeItemBase
{
    /// <summary>
    /// Создаёт гранату со встроенными параметрами по умолчанию.
    /// </summary>
    public JumpNade() : this(new GameplayItemCatalog())
    {
    }

    /// <summary>
    /// Создаёт гранату с параметрами из указанного runtime-каталога.
    /// </summary>
    public JumpNade(GameplayItemCatalog catalog)
        : base(catalog, GameplayItemKeys.JumpNade)
    {
    }

    private JumpNadeSettings Settings => (JumpNadeSettings)Definition.Settings;

    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        var settings = Settings;
        var alivePlayers = core.PlayerManager.GetAlive();
        var playersInRadius = Geometry.FindPlayersInSphere(alivePlayers, settings.Radius, position);

        foreach (var player in playersInRadius)
        {
            var playerPawn = player.PlayerPawn;

            if (playerPawn == null || !playerPawn.IsValid || playerPawn.AbsOrigin == null)
            {
                continue;
            }

            var distance = position.Distance(playerPawn.AbsOrigin.Value);
            var speed = settings.Power * (1f - distance / settings.Radius);
            var newVelocity = GetSpeedVector(position, playerPawn.AbsOrigin.Value, speed);

            playerPawn.AbsVelocity += newVelocity;
        }
    }

    private static Vector GetSpeedVector(Vector origin1, Vector origin2, float speed)
    {
        var direction = (origin2 - origin1).Normalized();
        var lengthSquared =
            direction.X * direction.X +
            direction.Y * direction.Y +
            direction.Z * direction.Z;

        if (lengthSquared <= 0.0001f)
        {
            return Vector.Zero;
        }

        var scale = speed / MathF.Sqrt(lengthSquared);
        var newVelocity = new Vector(
            direction.X * scale,
            direction.Y * scale,
            direction.Z * scale
        );

        if (direction.Z is < 0 and >= -3)
        {
            newVelocity.Z = scale;
        }

        return newVelocity;
    }
}
