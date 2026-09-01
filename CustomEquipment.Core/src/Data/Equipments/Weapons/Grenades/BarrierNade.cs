using Common.Di;
using Common.Math;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Data.Equipments.Weapons.Grenades;

/// <summary>
/// Представляет барьерную гранату с параметрами из PostgreSQL-каталога.
/// </summary>
public class BarrierNade : ManagedGrenadeItemBase
{
    /// <summary>
    /// Создаёт гранату со встроенными параметрами по умолчанию.
    /// </summary>
    public BarrierNade() : this(new GameplayItemCatalog())
    {
    }

    /// <summary>
    /// Создаёт гранату с параметрами из указанного runtime-каталога.
    /// </summary>
    public BarrierNade(GameplayItemCatalog catalog)
        : base(catalog, GameplayItemKeys.BarrierNade)
    {
    }

    private BarrierNadeSettings Settings => (BarrierNadeSettings)Definition.Settings;

    public override void OnDetonate(IPlayer thrower, Vector position)
    {
        var core = DependencyResolver.GetRequiredService<ISwiftlyCore>();
        CreateBarrier(core, position, Settings);
    }

    private static void CreateBarrier(
        ISwiftlyCore core,
        Vector position,
        BarrierNadeSettings settings
    )
    {
        var particle = core.EntitySystem.CreateEntity<CParticleSystem>();
        particle.StartActive = true;
        particle.EffectName = settings.Particle;
        particle.Teleport(position, null, null);
        particle.DispatchSpawn();

        SoundExt.PlayInPlace(
            particle,
            settings.EnvironmentSound,
            position,
            settings.EnvironmentVolume
        );

        CreateBarrierHandler(core, position, particle, settings);
    }

    private static void CreateBarrierHandler(
        ISwiftlyCore core,
        Vector position,
        CParticleSystem particle,
        BarrierNadeSettings settings
    )
    {
        var elapsedTime = 0f;
        CancellationTokenSource? token = null;

        token = core.Scheduler.RepeatBySeconds(settings.TickInterval, () =>
        {
            elapsedTime += settings.TickInterval;
            FindPlayersToKnock(core, position, settings);

            if (elapsedTime > settings.Duration)
            {
                DespawnBarrier(token, particle);
            }
        });
    }

    private static void FindPlayersToKnock(
        ISwiftlyCore core,
        Vector position,
        BarrierNadeSettings settings
    )
    {
        var alivePlayers = core.PlayerManager.GetTAlive();
        var playersInRadius = Geometry.FindPlayersInSphere(alivePlayers, settings.Radius, position);

        foreach (var player in playersInRadius)
        {
            Knock(player, position, settings);
        }
    }

    private static void DespawnBarrier(CancellationTokenSource? token, CParticleSystem particle)
    {
        if (particle.IsValidEntity)
        {
            particle.Despawn();
        }

        token?.Cancel();
    }

    private static void Knock(IPlayer player, Vector position, BarrierNadeSettings settings)
    {
        var pawn = player.PlayerPawn;

        if (pawn?.AbsOrigin == null)
        {
            return;
        }

        var origin = pawn.AbsOrigin.Value;
        var directionVector = (origin - position).Normalized();
        var onGround = pawn.GroundEntity.Value != null;
        var newVelocity = new Vector(
            pawn.AbsVelocity.X + directionVector.X * settings.HorizontalKnockback,
            pawn.AbsVelocity.Y + directionVector.Y * settings.HorizontalKnockback,
            onGround ? settings.GroundZBoost : settings.AirZBoost
        );

        pawn.GroundEntity.Value = null;
        pawn.Teleport(origin, pawn.EyeAngles, newVelocity);
        SoundExt.PlayAt(player, settings.KnockSound, 1);
    }
}
