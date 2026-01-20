using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Weapons.Grenades;

public class JumpNade(ISwiftlyCore core, CommonUtils utils) : ICustomWeapon, IGrenade
{
    public string OriginalName => "weapon_smoke";
    public string IternalName => "weapon_jump_nade";
    public string DisplayName => "Jump Nade";

    private const int ExplodeRadius = 250;
    private const int ExplodePower = 850;

    public void Load()
    {
        core.GameEvent.HookPre<EventSmokegrenadeDetonate>(PreEventGrenadeDetonate);
    }

    public void Explode(int userid, Vector position, int grenadeIndex)
    {
        var playersInRadius = utils.FindAllPlayersInSphere(ExplodeRadius, position);
        foreach (var player in playersInRadius)
        {
            var playerPawn = player.RequiredPlayerPawn;

            var distance = position.Distance(playerPawn.AbsOrigin!.Value);
            var speed = ExplodePower * (1.0f - (distance / ExplodeRadius));

            var newVelocity = GetSpeedVector(position, playerPawn.AbsOrigin!.Value, speed);
            playerPawn.AbsVelocity += newVelocity;
        }
    }

    private HookResult PreEventGrenadeDetonate(EventSmokegrenadeDetonate @event)
    {
        var grenade = core.EntitySystem.GetEntityByIndex<CBaseEntity>((uint)@event.EntityID);
        if (grenade is { IsValidEntity: true })
        {
            Explode(@event.UserId, new Vector(@event.X, @event.Y, @event.Z), @event.EntityID);
            grenade.Despawn();
        }

        return HookResult.Continue;
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