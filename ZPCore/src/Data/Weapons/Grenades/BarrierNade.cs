using ZPCore.Data.Extensions;
using ZPCore.Data.Weapons.Contracts;
using ZPCore.Data.Weapons.Enums;
using ZPCore.Data.Weapons.Utils;
using ZPCore.Di;
using ZPCore.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZPCore.Data.Weapons.Grenades;

internal sealed class BarrierNade : BaseGrenade, IWeaponPurchasable
{
    public override string InheritorName => WeaponName.Grenade.Decoy;

    public override string DisplayName => "Barrier Nade";

    public override string InternalName => "barrier_nade";

    public override WeaponSlot Slot => WeaponSlot.Grenades;

    public override string Model => "";

    public override WeaponRarity WeaponRarity => WeaponRarity.Modified;

    public int Coast => 1;

    public WeaponType WeaponType => WeaponType.Equipment;

    public override void OnDecoyStarted(Vector position)
    {
        var core = DependencyManager.GetService<ISwiftlyCore>();
        var startTime = 0f;
        
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        particle.StartActive = true;
        particle.EffectName = OnDecoyStartedParticleName;
        particle.Teleport(position, null, null);
        particle.DispatchSpawn();
        
        CancellationTokenSource token = null!;
        token = core.Scheduler.RepeatBySeconds(0.05f, () =>
        {
            startTime += 0.05f;
            
            var playersInRadius = MathAlgorithm.FindAllPlayersInSphere(175.0f, position);

            foreach (var player in playersInRadius)
            {
                if (player.IsInfected())
                {
                    Knock(player, position);
                }
            }

            if (startTime >= 15.0f)
            {
                if (particle is { IsValidEntity: true })
                {
                    particle.Despawn();
                }

                token.Cancel();
            }
        });
        
        base.OnDecoyStarted(position);
    }
    
    private void Knock(IPlayer player, Vector position)
    {
        var pawn = player.RequiredPlayerPawn;

        if (pawn.AbsOrigin == null)
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

    public override string OnDecoyStartedParticleName => "particles/barrier_nade.vpcf";
}