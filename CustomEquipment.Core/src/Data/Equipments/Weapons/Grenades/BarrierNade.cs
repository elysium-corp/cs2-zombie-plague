using Common.Di;
using Common.Math;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Data.Equipments.Weapons.Grenades;

public class BarrierNade : BaseGrenade
{
    public override string InheritorName => WeaponName.Smoke;

    public override string DisplayName => "Barrier Nade";

    public override Slot Slot => Slot.Grenade;

    public override WeaponType WeaponType => WeaponType.Grenade;

    public override string Model => "weapons/luci/elysium_smoke/elysium_smoke_ag2.vmdl";

    private const string BarrierParticleName = "particles/barrier_nade.vpcf";

    private const float BarrierRadius = 175.0f;

    private const float BarrierDuration = 15.0f;

    private const float TickDuration = 0.05f;

    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();

        CreateBarrier(core, position);
    }

    private void CreateBarrier(ISwiftlyCore core, Vector position)
    {
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        particle.StartActive = true;
        particle.EffectName = BarrierParticleName;
        particle.Teleport(position, null, null);
        particle.DispatchSpawn();

        CreateBarrierHandler(core, position, particle);
    }

    private void CreateBarrierHandler(ISwiftlyCore core, Vector position, CParticleSystem particle)
    {
        var elapsedTime = 0f;
        CancellationTokenSource? token = null;

        token = core.Scheduler.RepeatBySeconds(TickDuration, () =>
        {
            elapsedTime += TickDuration;

            FindPlayersToKnock(core, position);

            if (!IsActive(elapsedTime))
            {
                DespawnBarrier(token, particle);
            }
        });
    }

    private void FindPlayersToKnock(ISwiftlyCore core, Vector position)
    {
        var alivePlayers = core.PlayerManager.GetTAlive();
        var playersInRadius = Geometry.FindPlayersInSphere(alivePlayers, BarrierRadius, position);

        foreach (var player in playersInRadius)
        {
            Knock(player, position);
        }
    }

    private bool IsActive(float time)
    {
        return time <= BarrierDuration;
    }

    private void DespawnBarrier(CancellationTokenSource token, CParticleSystem particle)
    {
        if (particle.IsValidEntity)
        {
            particle.Despawn();
        }

        token.Cancel();
    }

    private void Knock(IPlayer player, Vector position)
    {
        var pawn = player.PlayerPawn;

        if (pawn?.AbsOrigin == null)
        {
            return;
        }

        var origin = pawn.AbsOrigin.Value;
        var directionVector = (origin - position).Normalized();
        var onGround = pawn.GroundEntity.Value != null;
        var zBoost = onGround ? 150f : 25f;
        var newVelocity = new Vector(
            pawn.AbsVelocity.X + directionVector.X * 200.0f,
            pawn.AbsVelocity.Y + directionVector.Y * 200.0f,
            zBoost
        );

        pawn.GroundEntity.Value = null;

        pawn.Teleport(origin, pawn.EyeAngles, newVelocity);
    }
}