using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using EventDelegates = SwiftlyS2.Shared.Events.EventDelegates;

namespace CustomEquipment.Controllers;

internal sealed class WeaponController(
    ISwiftlyCore core,
    IEquipmentService equipmentService,
    IParticleService particleService,
    ICustomEquipmentEvents events,
    IHookPublisher hooks
) : IWeaponController, IDisposable
{
    private Guid _guidBulletImpactPost = Guid.Empty;
    private bool _initialized;

    private readonly GrenadeHandler _grenadeHandler = new();

    private const float MinParticleLifetime = 0.1f;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        core.Event.OnTick += OnTick;
        core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;

        events.Grenades.Thrown.Hook(OnGrenadeThrown);

        _guidBulletImpactPost = core.GameEvent.HookPost<EventBulletImpact>(OnBulletImpactPost);
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        core.Event.OnTick -= OnTick;
        core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;

        events.Grenades.Thrown.Unhook(OnGrenadeThrown);

        if (_guidBulletImpactPost != Guid.Empty)
        {
            core.GameEvent.Unhook(_guidBulletImpactPost);
            _guidBulletImpactPost = Guid.Empty;
        }
        _grenadeHandler.Clear();
    }

    private void OnTick() =>
        _grenadeHandler.OnTick(OnGrenadeDetonated);

    private void OnGrenadeThrown(ref GrenadeThrownContext context) =>
        _grenadeHandler.OnGrenadeThrown(context.Grenade, context.Projectile);

    private void OnGrenadeDetonated(
        IGrenade grenade,
        CBaseCSGrenadeProjectile projectile,
        Vector position)
    {
        var preContext = new GrenadeDetonatingContext(grenade, projectile, position);

        if (!hooks.DispatchCancellable(ref preContext))
        {
            DispatchDetonationRejected(preContext, GrenadeDetonationRejectionReason.Cancelled);
            return;
        }

        if (preContext.Grenade is not GrenadeItemBase baseGrenade)
        {
            DispatchDetonationRejected(preContext, GrenadeDetonationRejectionReason.InvalidGrenade);
            return;
        }

        if (!preContext.Projectile.IsValidEntity)
        {
            DispatchDetonationRejected(preContext, GrenadeDetonationRejectionReason.InvalidProjectile);
            return;
        }

        var thrower = preContext.Projectile.OriginalThrower.Value?.ToPlayer();

        if (thrower == null || !thrower.IsValid)
        {
            DispatchDetonationRejected(preContext, GrenadeDetonationRejectionReason.InvalidThrower);
            return;
        }

        preContext.Projectile.Despawn();
        baseGrenade.OnDetonate(thrower, preContext.Position);

        var postContext = new GrenadeDetonatedContext(
            baseGrenade,
            preContext.Projectile,
            preContext.Position);
        hooks.Dispatch(ref postContext);
    }

    private void DispatchDetonationRejected(
        GrenadeDetonatingContext grenade,
        GrenadeDetonationRejectionReason reason
    )
    {
        var context = new GrenadeDetonationRejectedContext(
            grenade.Grenade,
            grenade.Projectile,
            reason
        );

        hooks.Dispatch(ref context);
    }

    private void OnEntityTakeDamage(ref TakeDamageEntityPreContext hook)
    {
        var attacker = hook.Params.Info.Attacker.ResolvePlayerFromHandle();
        var victim = hook.Params.Entity.Address.FindPlayerByPawnAddress();

        if (attacker == null || victim == null || !attacker.IsValid) return;

        var activeWeapon = equipmentService.GetActiveItem<WeaponItemBase>(attacker);

        if (activeWeapon == null || activeWeapon.WeaponDamage?.DamageMultiplier == null) return;

        var info = hook.Params.Info;
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
            _ => hook.Params.Info.Damage
        };

        var preContext = new WeaponDamageModifyingContext(
            attacker,
            victim,
            activeWeapon,
            baseDamage,
            damageModified
        );

        if (!hooks.DispatchCancellable(ref preContext))
        {
            return;
        }

        hook.Params.Info.Damage = preContext.Damage;

        var postContext = new WeaponDamageModifiedContext(
            attacker,
            victim,
            activeWeapon,
            baseDamage,
            preContext.Damage
        );

        hooks.Dispatch(ref postContext);
    }

    private HookResult OnBulletImpactPost(EventBulletImpact hook)
    {
        var attacker = hook.UserIdPlayer;

        if (attacker == null || !attacker.IsValid) return HookResult.Continue;

        var activeWeapon = equipmentService.GetActiveItem<WeaponItemBase>(attacker);

        if (activeWeapon == null) return HookResult.Continue;

        var impactPos = new Vector(hook.X, hook.Y, hook.Z);
        var attachedWeapon = activeWeapon.AttachedWeapon;

        var preContext = new WeaponImpactProcessingContext(attacker, activeWeapon, impactPos);

        if (!hooks.DispatchCancellable(ref preContext))
        {
            return HookResult.Continue;
        }

        impactPos = preContext.Position;

        if (activeWeapon.HasTraceParticle())
        {
            particleService.CreateTracerParticle(activeWeapon.Particle.Trace, attachedWeapon, impactPos,
                MinParticleLifetime);
        }

        if (activeWeapon.HasMuzzleFlashParticle())
        {
            particleService.CreateParticleAttached(activeWeapon.Particle.MuzzleFlash, attachedWeapon,
                Attachment.MuzzleFlash, MinParticleLifetime);
        }

        if (activeWeapon.HasImpactParticle())
        {
            particleService.CreateParticle(activeWeapon.Particle.Impact, impactPos, MinParticleLifetime);
        }

        var postContext = new WeaponImpactProcessedContext(attacker, activeWeapon, impactPos);
        hooks.Dispatch(ref postContext);

        return HookResult.Continue;
    }
}
