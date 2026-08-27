using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using Economy.Api;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;
using EventDelegates = SwiftlyS2.Shared.Events.EventDelegates;

namespace CustomEquipment.Controllers;

internal sealed class WeaponController(
    ISwiftlyCore core,
    IEquipmentService equipmentService,
    IParticleService particleService,
    ICustomEquipmentEvents events,
    IHookPublisher hooks,
    IEconomyApi economyApi
) : IWeaponController, IDisposable
{
    private Guid _guidBulletImpactPost = Guid.Empty;

    private readonly GrenadeHandler _grenadeHandler = new();

    private const float MinParticleLifetime = 0.1f;

    private static readonly List<string> BuySounds =
        ["ZombiePlague.ammo_buy_01", "ZombiePlague.ammo_buy_02", "ZombiePlague.ammo_buy_03"];

    private const string CancelSound = "ZombiePlague.cancel";

    public void Initialize()
    {
        core.Event.OnTick += OnTick;
        core.GameHooks.Entities.TakeDamage.Pre += OnEntityTakeDamage;
        core.Event.OnClientKeyStateChanged += OnClientKeyStateChanged;

        events.Post.GrenadeThrowEvent += OnGrenadeThrown;

        _guidBulletImpactPost = core.GameEvent.HookPost<EventBulletImpact>(OnBulletImpactPost);
    }

    public void Dispose()
    {
        core.Event.OnTick -= OnTick;
        core.GameHooks.Entities.TakeDamage.Pre -= OnEntityTakeDamage;
        core.Event.OnClientKeyStateChanged -= OnClientKeyStateChanged;

        events.Post.GrenadeThrowEvent -= OnGrenadeThrown;

        core.GameEvent.Unhook(_guidBulletImpactPost);
    }

    private void OnTick() =>
        _grenadeHandler.OnTick(OnGrenadeDetonated);

    private void OnGrenadeThrown(ref GrenadeThrowPostContext context) =>
        _grenadeHandler.OnGrenadeThrown(context.Grenade, context.Projectile);

    private void OnGrenadeDetonated(
        IGrenade grenade,
        CBaseCSGrenadeProjectile projectile,
        Vector position)
    {
        var preContext = new GrenadeDetonatePreContext(grenade, projectile, position);

        if (!hooks.DispatchCancellable(ref preContext) ||
            preContext.Grenade is not GrenadeItemBase baseGrenade ||
            preContext.Projectile is null ||
            !preContext.Projectile.IsValidEntity)
        {
            return;
        }

        var thrower = preContext.Projectile.OriginalThrower.Value?.ToPlayer();

        if (thrower == null || !thrower.IsValid)
        {
            return;
        }

        preContext.Projectile.Despawn();
        baseGrenade.OnDetonate(thrower, preContext.Position);

        var postContext = new GrenadeDetonatePostContext(
            baseGrenade,
            preContext.Projectile,
            preContext.Position);
        hooks.Dispatch(ref postContext);
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

        hook.Params.Info.Damage = damageModified;
    }

    private void OnClientKeyStateChanged(IOnClientKeyStateChangedEvent @event)
    {
        var player = core.PlayerManager.GetPlayer(@event.PlayerId);

        if (player == null) return;

        if (player.IsFakeClient) return;

        var playerPawn = player.PlayerPawn;

        if (playerPawn == null || !playerPawn.IsValid) return;

        if (@event.Key != KeyKind.E || !@event.Pressed) return;

        var weapon = equipmentService.GetActiveItem<WeaponItemBase>(player);

        var shopItem = weapon as IShopItem;

        if (shopItem == null) return;

        var reserveAmmo = weapon?.AttachedWeapon.ReserveAmmo[0];
        var maxReserveAmmo = weapon?.Ammunition?.ReserveAmmo;
        var ammoPrice = shopItem.Price.Ammo;

        if (!ammoPrice.HasValue || !maxReserveAmmo.HasValue)
        {
            return;
        }

        if (reserveAmmo >= maxReserveAmmo.Value)
        {
            SoundExt.PlayLocalSound(player, CancelSound, 1f);

            return;
        }

        if (economyApi.TrySpendMoney(player, ammoPrice.Value))
        {
            weapon?.AttachedWeapon.ReserveAmmo[0] += 1;
            weapon?.AttachedWeapon.ReserveAmmoUpdated();

            SoundExt.PlayLocalSound(player, BuySounds.GetRandomString(), 1f);
        }
    }

    private HookResult OnBulletImpactPost(EventBulletImpact hook)
    {
        var attacker = hook.UserIdPlayer;

        if (attacker == null || !attacker.IsValid) return HookResult.Continue;

        var activeWeapon = equipmentService.GetActiveItem<WeaponItemBase>(attacker);

        if (activeWeapon == null) return HookResult.Continue;

        var impactPos = new Vector(hook.X, hook.Y, hook.Z);
        var attachedWeapon = activeWeapon.AttachedWeapon;

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

        return HookResult.Continue;
    }
}
