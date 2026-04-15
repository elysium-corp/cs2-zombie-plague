using CustomEquipment.Api;
using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using IEventSubscriber = CustomEquipment.Api.IEventSubscriber;

namespace CustomEquipment.Controllers;

internal sealed class WeaponController(
    ISwiftlyCore core, 
    IEquipmentService equipmentService, 
    IParticleService particleService, 
    IEventSubscriber eventSubscriber, 
    IEventPublisher eventPublisher
    ) : IWeaponController, IDisposable
{
    private Guid _guidBulletImpactPost = Guid.Empty;
    
    private readonly GrenadeHandler _grenadeHandler = new(eventPublisher);

    private const float MinParticleLifetime = 0.1f;
    
    public void Initialize()
    {
        core.Event.OnTick += OnTick;
        core.Event.OnEntityTakeDamage += OnEntityTakeDamage;

        eventSubscriber.OnGrenadeThrown += OnGrenadeThrown;
        eventSubscriber.OnGrenadeDetonated += OnGrenadeDetonated;
        
        _guidBulletImpactPost = core.GameEvent.HookPost<EventBulletImpact>(OnBulletImpactPost);
    }

    public void Dispose()
    {
        core.Event.OnTick -= OnTick;
        core.Event.OnEntityTakeDamage -= OnEntityTakeDamage;
        
        eventSubscriber.OnGrenadeThrown -= OnGrenadeThrown;
        eventSubscriber.OnGrenadeDetonated -= OnGrenadeDetonated;
        
        core.GameEvent.Unhook(_guidBulletImpactPost);
    }
    
    private void OnTick() =>
        _grenadeHandler.OnTick();

    private void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile) =>
        _grenadeHandler.OnGrenadeThrown(grenade, projectile);

    private void OnGrenadeDetonated(IGrenade grenade, CBaseCSGrenadeProjectile projectile, Vector position)
    {
        _grenadeHandler.OnGrenadeDetonated(grenade, projectile, position);
        
        if (grenade is not BaseGrenade baseGrenade) return;

        var thrower = projectile.OriginalThrower.Value?.ToPlayer();

        if (thrower == null) return;

        projectile.Despawn();
        
        baseGrenade.OnDetonate(thrower, position);
    }

    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent hook)
    {
        var attacker = hook.Info.Attacker.ResolvePlayerFromHandle();
        var victim = hook.Entity.Address.FindPlayerByPawnAddress();

        if (attacker == null || victim == null || !attacker.IsValid) return;

        var activeWeapon = equipmentService.GetActiveItem<BaseWeapon>(attacker);

        if (activeWeapon == null || activeWeapon.WeaponDamage?.DamageMultiplier == null) return;

        var info = hook.Info;
        var damageMultiplier = activeWeapon.WeaponDamage.DamageMultiplier;
        var baseDamage = info.Damage;

        var damageModified = info.ActualHitGroup switch
        {
            HitGroup_t.HITGROUP_HEAD => baseDamage * damageMultiplier.Head,
            HitGroup_t.HITGROUP_CHEST => baseDamage * damageMultiplier.Chest,
            HitGroup_t.HITGROUP_STOMACH => baseDamage * damageMultiplier.Stomach,
            HitGroup_t.HITGROUP_LEFTARM => baseDamage * damageMultiplier.Arms.Left,
            HitGroup_t.HITGROUP_RIGHTARM => baseDamage * damageMultiplier.Arms.Right,
            HitGroup_t.HITGROUP_LEFTLEG => baseDamage * damageMultiplier.Legs.Left,
            HitGroup_t.HITGROUP_RIGHTLEG => baseDamage * damageMultiplier.Legs.Right,
            HitGroup_t.HITGROUP_NECK => baseDamage * damageMultiplier.Neck,
            _ => hook.Info.Damage
        };

        hook.Info.Damage = damageModified;
    }
    
    private HookResult OnBulletImpactPost(EventBulletImpact hook)
    {
        var attacker = hook.UserIdPlayer;

        if (attacker == null || !attacker.IsValid) return HookResult.Continue;

        var activeWeapon = equipmentService.GetActiveWeapon<BaseWeapon>(attacker);
        
        if (activeWeapon == null) return HookResult.Continue;
        
        var impactPos = new Vector(hook.X, hook.Y, hook.Z);
        var attachedWeapon = activeWeapon.AttachedWeapon;
        
        if (activeWeapon.HasTraceParticle())
        {
            particleService.CreateTracerParticle(activeWeapon.Particle.Trace, attachedWeapon, impactPos, MinParticleLifetime);
        }

        if (activeWeapon.HasMuzzleFlashParticle())
        {
            particleService.CreateParticleAttached(activeWeapon.Particle.MuzzleFlash, attachedWeapon, Attachment.MuzzleFlash, MinParticleLifetime);
        }

        if (activeWeapon.HasImpactParticle())
        {
            particleService.CreateParticle(activeWeapon.Particle.Impact, impactPos, MinParticleLifetime);
        }
        
        return HookResult.Continue;
    }
}