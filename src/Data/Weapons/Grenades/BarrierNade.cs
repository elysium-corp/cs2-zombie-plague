using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Vector = SwiftlyS2.Shared.Natives.Vector;

namespace CS2ZombiePlague.Data.Weapons.Grenades;

public class BarrierNade(ISwiftlyCore core, RoundManager roundManager, CommonUtils commonUtils) : ICustomWeapon, IGrenade
{
    public string OriginalName => "decoy_grenade";
    public string IternalName => "barrier_nade";
    public string DisplayName => "BarrierNade";

    private const float ExplodeRadius = 175.0f;
    private const float LiveTime = 15.0f;
    private const float KnockbackDistance = 200.0f;
    private const float Delay = 0.05f;
    private const float StartTime = 0f;
    private const float HighZBoost = 150f;
    private const float LowZBoost = 25f;
    
    private const string ParticleEffectName = "particles/barrier_nade.vpcf";

    public void Load()
    {
        core.GameEvent.HookPre<EventDecoyStarted>(PreEventGrenadeStarted);
    }

    public void Explode(int userid, Vector position, int grenadeIndex)
    {
        var startTime = StartTime;

        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        particle.StartActive = true;
        particle.EffectName = ParticleEffectName;
        particle.Teleport(position, null, null);
        particle.DispatchSpawn();
        
        CancellationTokenSource token = null!;

        token = core.Scheduler.RepeatBySeconds(Delay, () =>
        {
            startTime += Delay;
            
            var playersInRadius = commonUtils.FindAllPlayersInSphere(ExplodeRadius, position);

            foreach (var player in playersInRadius)
            {
                if (player.IsInfected())
                {
                    Knock(player, position);
                }
            }

            if (startTime >= LiveTime)
            {
                if (particle is { IsValidEntity: true })
                {
                    particle.Despawn();
                }

                token?.Cancel();
            }
        });
    }

    private HookResult PreEventGrenadeStarted(EventDecoyStarted @event)
    {
        var grenade = core.EntitySystem.GetEntityByIndex<CEntityInstance>((uint)@event.EntityID);
        if (grenade is { IsValid: true })
        {
            Explode(@event.UserId, new Vector(@event.X, @event.Y, @event.Z), (int)grenade.Index);
            grenade.Despawn();
        }

        return HookResult.Stop;
    }

    private void Knock(IPlayer player, Vector position)
    {
        var playerPawn = player.PlayerPawn;
        if (playerPawn == null)
        {
            return;
        }

        var directionVector = (playerPawn.AbsOrigin.Value - position).Normalized();

        bool onGround = playerPawn.GroundEntity.Value != null;
        var zBoost = onGround ? HighZBoost : LowZBoost;

        Vector newVelocity = new Vector(
            playerPawn.AbsVelocity.X + directionVector.X * KnockbackDistance,
            playerPawn.AbsVelocity.Y + directionVector.Y * KnockbackDistance,
            zBoost
        );

        playerPawn.GroundEntity.Value = null;

        playerPawn.Teleport(playerPawn.AbsOrigin.Value, playerPawn.EyeAngles, newVelocity);
    }
}