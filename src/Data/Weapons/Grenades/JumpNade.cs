using CS2ZombiePlague.Data.Weapons.Contracts;
using CS2ZombiePlague.Data.Weapons.Enums;
using CS2ZombiePlague.Data.Weapons.Utils;
using CS2ZombiePlague.Di;
using SwiftlyS2.Shared.Natives;

namespace CS2ZombiePlague.Data.Weapons.Grenades;

public sealed class JumpNade : BaseGrenade, IWeaponPurchasable
{
    public override string InheritorName => WeaponName.Grenade.Smoke;

    public override string DisplayName => "Jump Nade";

    public override string InternalName => "jump_nade";

    public override WeaponSlot Slot => WeaponSlot.Grenades;

    public override string Model => "";

    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.Equipment;

    public override void OnSmokegrenadeDetonate(Vector position)
    {
        var commonUtils = DependencyManager.GetService<CommonUtils>();
        var playersInRadius = commonUtils.FindAllPlayersInSphere(250.0f, position);
        
        foreach (var player in playersInRadius)
        {
            var playerPawn = player.RequiredPlayerPawn;
            var distance = position.Distance(playerPawn.AbsOrigin!.Value);
            var speed = 850.0f * (1.0f - distance / 250.0f);
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

        if (lengthSquared <= 0.0001f) return Vector.Zero;

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