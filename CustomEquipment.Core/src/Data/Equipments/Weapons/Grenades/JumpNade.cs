using Common.Di;
using Common.Math;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Weapons.Grenades;

public sealed class JumpNade : GrenadeItemBase
{
    public override string InheritorName => WeaponName.He;

    public override string DisplayName => "Jump Nade";

    public override string InternalName => "custom_equipment:jump_nade";

    public override Slot Slot => Slot.Grenade;

    public override WeaponType WeaponType => WeaponType.Grenade;

    public override string Model => "models/throwhead/throwhead2_ag2.vmdl";

    private const int ExplodeRadius = 250;

    private const int ExplodePower = 1050;

    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();

        var alivePlayers = core.PlayerManager.GetAlive();
        var playersInRadius = Geometry.FindPlayersInSphere(alivePlayers, ExplodeRadius, position);

        foreach (var player in playersInRadius)
        {
            var playerPawn = player.PlayerPawn;
            
            if(playerPawn == null || !playerPawn.IsValid) break;
            
            var distance = position.Distance(playerPawn.AbsOrigin!.Value);
            var speed = ExplodePower * (1.0f - distance / ExplodeRadius);
            var newVelocity = GetSpeedVector(position, playerPawn.AbsOrigin!.Value, speed);
            
            playerPawn.AbsVelocity += newVelocity;
        }
    }

    private Vector GetSpeedVector(Vector origin1, Vector origin2, float speed)
    {
        Vector direction = (origin2 - origin1).Normalized();

        float lengthSquared =
            direction.X * direction.X +
            direction.Y * direction.Y +
            direction.Z * direction.Z;

        if (lengthSquared <= 0.0001f)
            return Vector.Zero;

        float scale = speed / MathF.Sqrt(lengthSquared);

        var newVelocity = new Vector(
            direction.X * scale,
            direction.Y * scale,
            direction.Z * scale
        );
        if (direction.Z < 0 && direction.Z >= -3)
        {
            newVelocity.Z = scale;
        }

        return newVelocity;
    }
}