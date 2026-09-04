using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Giver;
using CustomEquipment.Policies;
using CustomEquipment.Registry;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;

namespace CustomEquipment.Services;

internal sealed class EquipmentService(
    ISwiftlyCore core,
    IItemGiver itemGiver,
    IItemRegistry itemRegistry,
    IHookPublisher hooks,
    Func<IZombiePlagueApi> zombiePlagueApi
) : IEquipmentService, IDisposable
{
    private const string C4DesignerName = "weapon_" + WeaponName.C4;
    private const string LegacyLaserMineDesignerName = "weapon_healthshot";
    private const string PlantedC4DesignerName = "planted_c4";
    private readonly List<ItemBase> _items = [];
    private readonly HashSet<int> _laserMineGrantPlayers = [];
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        core.Event.OnEntityCreated += OnEntityCreated;
        core.Event.OnEntityDeleted += OnEntityDeleted;
        core.Event.OnMapLoad += OnMapLoad;

        core.GameHooks.Weapons.CanUse.Pre += OnWeaponCanUsePre;

        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Hook(OnPlayerInfected);
        playerEvents.Disinfected.Hook(OnPlayerDisinfected);
        playerEvents.Humanized.Hook(OnPlayerHumanized);
        playerEvents.BecameNemesis.Hook(OnPlayerBecameNemesis);
        playerEvents.BecameSurvivor.Hook(OnPlayerBecameSurvivor);

        core.Scheduler.NextWorldUpdate(RemoveForbiddenBombsAndLegacyCarriers);
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        core.Event.OnEntityCreated -= OnEntityCreated;
        core.Event.OnEntityDeleted -= OnEntityDeleted;
        core.Event.OnMapLoad -= OnMapLoad;

        core.GameHooks.Weapons.CanUse.Pre -= OnWeaponCanUsePre;

        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Unhook(OnPlayerInfected);
        playerEvents.Disinfected.Unhook(OnPlayerDisinfected);
        playerEvents.Humanized.Unhook(OnPlayerHumanized);
        playerEvents.BecameNemesis.Unhook(OnPlayerBecameNemesis);
        playerEvents.BecameSurvivor.Unhook(OnPlayerBecameSurvivor);

        _items.Clear();
        _laserMineGrantPlayers.Clear();
    }

    public bool CanUseItem(IPlayer player, ItemBase item)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!itemRegistry.TryGetDefinition(item.InternalName, out var definition))
        {
            return false;
        }

        if (definition is not ItemBase)
        {
            return false;
        }

        return CanUseItemInternal(player, item);
    }

    public bool CanUseItem(IPlayer player, string name)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!itemRegistry.TryGetDefinition(name, out var definition))
        {
            return false;
        }

        if (definition is not ItemBase item)
        {
            return false;
        }

        return CanUseItemInternal(player, item);
    }

    private void OnEntityItemGiven(IPlayer player, ItemBase item, GiveAction action)
    {
        AddOrReplace(item);
        DispatchItemGiven(player, item, action);

        if (!CanUseItemInternal(player, item))
        {
            RemoveItem(player, item);
        }
    }

    private void DispatchItemGiven(IPlayer player, ItemBase item, GiveAction action)
    {
        switch (item)
        {
            case WeaponItemBase weapon:
                var weaponPost = new WeaponGivenContext(player, weapon, action);
                hooks.Dispatch(ref weaponPost);
                break;

            case GrenadeItemBase grenade:
                var grenadePost = new GrenadeGivenContext(player, grenade, action);
                hooks.Dispatch(ref grenadePost);
                break;
        }

        var itemPost = new ItemGivenContext(player, item, action);
        hooks.Dispatch(ref itemPost);
    }

    public bool TryGiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.IsValid)
        {
            DispatchGiveRejected(player, internalName, null, action, ItemGiveRejectionReason.InvalidPlayer);
            return false;
        }

        if (!CanUseItem(player, internalName))
        {
            DispatchGiveRejected(player, internalName, null, action, ItemGiveRejectionReason.CannotUse);
            return false;
        }

        ItemBase item;

        try
        {
            if (itemRegistry.Create(internalName) is not ItemBase createdItem)
            {
                throw new InvalidOperationException($"Item '{internalName}' is not an ItemBase!");
            }

            item = createdItem;
        }
        catch (Exception exception)
        {
            DispatchGiveFailed(player, internalName, action, exception);
            throw;
        }

        var itemPre = new ItemGivingContext(player, item, action);

        if (!hooks.DispatchCancellable(ref itemPre))
        {
            DispatchGiveRejected(player, internalName, item, action, ItemGiveRejectionReason.Cancelled);
            return false;
        }

        if (itemPre.Item is not ItemBase preparedItem ||
            !itemPre.Player.IsValid ||
            !CanUseItem(itemPre.Player, preparedItem))
        {
            DispatchGiveRejected(
                itemPre.Player,
                internalName,
                itemPre.Item,
                itemPre.Action,
                ItemGiveRejectionReason.InvalidReplacement
            );
            return false;
        }

        var preparedPlayer = itemPre.Player;
        var preparedAction = itemPre.Action;

        switch (preparedItem)
        {
            case WeaponItemBase weapon:
            {
                var weaponPre = new WeaponGivingContext(preparedPlayer, weapon, preparedAction);

                if (!hooks.DispatchCancellable(ref weaponPre))
                {
                    DispatchGiveRejected(
                        weaponPre.Player,
                        internalName,
                        weaponPre.Weapon,
                        weaponPre.Action,
                        ItemGiveRejectionReason.TypeSpecificCancelled
                    );
                    return false;
                }

                if (weaponPre.Weapon is not WeaponItemBase preparedWeapon ||
                    !weaponPre.Player.IsValid ||
                    !CanUseItem(weaponPre.Player, preparedWeapon))
                {
                    DispatchGiveRejected(
                        weaponPre.Player,
                        internalName,
                        weaponPre.Weapon,
                        weaponPre.Action,
                        ItemGiveRejectionReason.InvalidReplacement
                    );
                    return false;
                }

                preparedPlayer = weaponPre.Player;
                preparedItem = preparedWeapon;
                preparedAction = weaponPre.Action;
                break;
            }

            case GrenadeItemBase grenade:
            {
                var grenadePre = new GrenadeGivingContext(preparedPlayer, grenade, preparedAction);

                if (!hooks.DispatchCancellable(ref grenadePre))
                {
                    DispatchGiveRejected(
                        grenadePre.Player,
                        internalName,
                        grenadePre.Grenade,
                        grenadePre.Action,
                        ItemGiveRejectionReason.TypeSpecificCancelled
                    );
                    return false;
                }

                if (grenadePre.Grenade is not GrenadeItemBase preparedGrenade ||
                    !grenadePre.Player.IsValid ||
                    !CanUseItem(grenadePre.Player, preparedGrenade))
                {
                    DispatchGiveRejected(
                        grenadePre.Player,
                        internalName,
                        grenadePre.Grenade,
                        grenadePre.Action,
                        ItemGiveRejectionReason.InvalidReplacement
                    );
                    return false;
                }

                preparedPlayer = grenadePre.Player;
                preparedItem = preparedGrenade;
                preparedAction = grenadePre.Action;
                break;
            }
        }

        int? laserMineGrantPlayerId = null;

        if (preparedItem is LaserMine)
        {
            var playerId = preparedPlayer.PlayerID;
            var hasCarriedMine = HasItem<LaserMine>(preparedPlayer);
            var grantInProgress = _laserMineGrantPlayers.Contains(playerId);

            if (!LaserMinePolicy.CanGrant(hasCarriedMine, grantInProgress) ||
                !_laserMineGrantPlayers.Add(playerId))
            {
                DispatchGiveRejected(
                    preparedPlayer,
                    internalName,
                    preparedItem,
                    preparedAction,
                    ItemGiveRejectionReason.AlreadyOwned
                );
                return false;
            }

            laserMineGrantPlayerId = playerId;
        }

        try
        {
            itemGiver.GiveItem(
                preparedPlayer,
                preparedItem,
                preparedAction,
                completedItem => OnEntityItemGiven(preparedPlayer, completedItem, preparedAction)
            );
        }
        catch (Exception exception)
        {
            DispatchGiveFailed(preparedPlayer, internalName, preparedAction, exception);
            throw;
        }
        finally
        {
            if (laserMineGrantPlayerId.HasValue)
            {
                _laserMineGrantPlayers.Remove(laserMineGrantPlayerId.Value);
            }
        }

        return true;
    }

    private void DispatchGiveRejected(
        IPlayer player,
        string internalName,
        IItem? item,
        GiveAction action,
        ItemGiveRejectionReason reason
    )
    {
        var context = new ItemGiveRejectedContext(player, internalName, item, action, reason);
        hooks.Dispatch(ref context);
    }

    private void DispatchGiveFailed(
        IPlayer player,
        string internalName,
        GiveAction action,
        Exception exception
    )
    {
        var context = new ItemGiveFailedContext(player, internalName, action, exception);
        hooks.Dispatch(ref context);
    }

    public TItem? GetActiveItem<TItem>(IPlayer player) where TItem : ItemBase
    {
        if (!player.IsValid)
        {
            return null;
        }

        var activeWeaponIndex = player.PlayerPawn?.WeaponServices?.ActiveWeapon.Value?.Index;

        if (activeWeaponIndex == null) return null;

        return _items.Find(wp => wp.AttachedEntity.Index == activeWeaponIndex) as TItem;
    }

    public bool TryRefillActiveWeapon(
        IPlayer player,
        string expectedInternalName,
        int amount,
        out AmmoRefillResult result)
    {
        result = default;

        if (amount <= 0 ||
            GetActiveItem<WeaponItemBase>(player) is not { } weapon ||
            !weapon.InternalName.Equals(expectedInternalName, StringComparison.OrdinalIgnoreCase) ||
            weapon.Ammunition?.ReserveAmmo is not { } maximumReserve ||
            maximumReserve <= 0)
        {
            return false;
        }

        var currentReserve = Math.Clamp(weapon.AttachedWeapon.ReserveAmmo[0], 0, maximumReserve);
        if (currentReserve >= maximumReserve)
        {
            return false;
        }

        var updatedReserve = (int)Math.Min((long)currentReserve + amount, maximumReserve);
        weapon.AttachedWeapon.ReserveAmmo[0] = updatedReserve;
        weapon.AttachedWeapon.ReserveAmmoUpdated();
        result = new AmmoRefillResult(updatedReserve - currentReserve, updatedReserve);
        return true;
    }

    public bool CanRefillActiveWeapon(IPlayer player, string expectedInternalName)
    {
        if (GetActiveItem<WeaponItemBase>(player) is not { } weapon ||
            !weapon.InternalName.Equals(expectedInternalName, StringComparison.OrdinalIgnoreCase) ||
            weapon.Ammunition?.ReserveAmmo is not { } maximumReserve ||
            maximumReserve <= 0)
        {
            return false;
        }

        return weapon.AttachedWeapon.ReserveAmmo[0] < maximumReserve;
    }

    public bool HasItem<TItem>(IPlayer player) where TItem : ItemBase
    {
        return GetPlayerItems<TItem>(player).Count > 0;
    }

    public int RemoveItems<TItem>(IPlayer player) where TItem : ItemBase
    {
        var items = GetPlayerItems<TItem>(player);

        foreach (var item in items)
        {
            RemoveItem(player, item);
        }

        return items.Count;
    }

    public int RemoveInaccessibleItems(IPlayer player)
    {
        var items = GetPlayerItems<ItemBase>(player)
            .Where(item => !CanUseItemInternal(player, item))
            .ToArray();

        foreach (var item in items)
        {
            RemoveItem(player, item);
        }

        return items.Length;
    }

    public TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponItemBase
    {
        var activeWeaponIndex = player.RequiredPawn.WeaponServices?.ActiveWeapon.Value?.Index;

        if (activeWeaponIndex == null) return null;

        return _items.Find(wp => wp.AttachedEntity.Index == activeWeaponIndex) as TWeapon;
    }

    public WeaponItemBase? GetWeaponByEntityIndex(uint entityIndex)
    {
        return GetWeaponByIndex(entityIndex);
    }

    public TItem? GetItemByEntityIndex<TItem>(uint entityIndex) where TItem : ItemBase
    {
        return GetItemByIndex(entityIndex) as TItem;
    }

    private void OnEntityCreated(IOnEntityCreatedEvent hook)
    {
        var entity = hook.Entity;

        if (!entity.IsValid) return;

        if (IsForbiddenEntityCandidate(entity))
        {
            core.Scheduler.NextWorldUpdate(() => RemoveIfForbidden(entity));
        }

        if (entity is not CBaseCSGrenadeProjectile) return;

        core.Scheduler.NextWorldUpdate(() =>
        {
            var projectile = entity.As<CBaseCSGrenadeProjectile>();
            var grenade = ResolveGrenadeByProjectile(projectile);

            if (grenade == null) return;

            var preContext = new GrenadeThrowingContext(grenade, projectile);

            if (!hooks.DispatchCancellable(ref preContext))
            {
                DispatchGrenadeThrowRejected(
                    preContext.Grenade,
                    preContext.Projectile,
                    GrenadeThrowRejectionReason.Cancelled
                );
                return;
            }

            if (!preContext.Projectile.IsValidEntity)
            {
                DispatchGrenadeThrowRejected(
                    preContext.Grenade,
                    preContext.Projectile,
                    GrenadeThrowRejectionReason.InvalidProjectile
                );
                return;
            }

            preContext.Projectile.SetModel(preContext.Grenade.Model);

            var postContext = new GrenadeThrownContext(preContext.Grenade, preContext.Projectile);
            hooks.Dispatch(ref postContext);
        });
    }

    private void DispatchGrenadeThrowRejected(
        IGrenade grenade,
        CBaseCSGrenadeProjectile projectile,
        GrenadeThrowRejectionReason reason
    )
    {
        var context = new GrenadeThrowRejectedContext(grenade, projectile, reason);
        hooks.Dispatch(ref context);
    }

    private void OnEntityDeleted(IOnEntityDeletedEvent hook)
    {
        var entity = hook.Entity;

        _items.RemoveAll(wp => wp.AttachedEntity.Index == entity.Index);
    }

    private void OnWeaponCanUsePre(ref CanUseWeaponPreContext context)
    {
        var weapon = context.Params.Weapon;
        var customItem = GetItemByIndex(weapon.Index);

        if (weapon.DesignerName == C4DesignerName)
        {
            var isLaserMine = customItem is LaserMine;
            // GiveNamedItem may call CanUse before OnItemGiven registers the new entity.
            var grantInProgress = _laserMineGrantPlayers.Contains(context.Params.Player.PlayerID);
            var accessAllowed = customItem is LaserMine laserMine &&
                                CanUseItemInternal(context.Params.Player, laserMine);
            var hasOtherCarriedMine = isLaserMine &&
                                      HasOtherLaserMine(context.Params.Player, weapon.Index);

            if (LaserMinePolicy.CanUseC4(
                    isLaserMine,
                    grantInProgress,
                    accessAllowed,
                    hasOtherCarriedMine
                ))
            {
                return;
            }

            DenyWeaponUse(ref context);
            return;
        }

        if (customItem is null || CanUseItem(context.Params.Player, customItem))
        {
            return;
        }

        DenyWeaponUse(ref context);
    }

    private WeaponItemBase? GetWeaponByIndex(uint index)
    {
        return _items
            .OfType<WeaponItemBase>()
            .FirstOrDefault(weapon => weapon.AttachedEntity.Index == index);
    }

    private ItemBase? GetItemByIndex(uint index)
    {
        return _items.FirstOrDefault(item => item.AttachedEntity.Index == index);
    }

    private bool HasOtherLaserMine(IPlayer player, uint entityIndex)
    {
        return GetPlayerItems<LaserMine>(player)
            .Any(item => item.AttachedEntity.Index != entityIndex);
    }

    private GrenadeItemBase? GetGrenadeByIndex(uint index)
    {
        return _items
            .OfType<GrenadeItemBase>()
            .FirstOrDefault(grenade => grenade.AttachedEntity.Index == index);
    }

    private GrenadeItemBase? ResolveGrenadeByProjectile(CBaseCSGrenadeProjectile projectile)
    {
        var thrower = projectile.Thrower.Value;

        if (thrower == null || !thrower.IsValid) return null;

        if (projectile is CMolotovProjectile { IsIncGrenade: true })
        {
            var incenderiary = thrower.WeaponServices?.FindWeaponByName(WeaponName.Inc);

            if (incenderiary == null) return null;

            return GetGrenadeByIndex(incenderiary.Index);
        }

        var simpleProjectile = projectile.DesignerName.Replace("_projectile", "");
        var grenade = thrower.WeaponServices?.FindWeaponByName(simpleProjectile);

        if (grenade == null) return null;

        return GetGrenadeByIndex(grenade.Index);
    }

    private TItem AddOrReplace<TItem>(TItem item) where TItem : ItemBase
    {
        var index = _items.FindIndex(current => current.AttachedEntity.Index == item.AttachedEntity.Index);

        if (index < 0)
        {
            _items.Add(item);
        }
        else
        {
            _items[index] = item;
        }

        return item;
    }

    private IReadOnlyList<TItem> GetPlayerItems<TItem>(IPlayer player) where TItem : ItemBase
    {
        if (!player.IsValid || player.PlayerPawn?.WeaponServices is not { } weaponServices)
        {
            return [];
        }

        var entityIndexes = weaponServices.MyValidWeapons
            .Select(weapon => weapon.Index)
            .ToHashSet();

        return _items
            .OfType<TItem>()
            .Where(item => entityIndexes.Contains(item.AttachedEntity.Index))
            .ToArray();
    }

    private void RemoveItem(IPlayer player, ItemBase item)
    {
        var weaponServices = player.PlayerPawn?.WeaponServices;
        var weapon = item.AttachedEntity.As<CBasePlayerWeapon>();

        if (weaponServices is not null && weapon is { IsValidEntity: true })
        {
            weaponServices.RemoveWeapon(weapon);
        }

        _items.Remove(item);
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context)
    {
        RemoveInaccessibleItems(context.Player);
    }

    private void OnPlayerDisinfected(ref PlayerDisinfectedContext context)
    {
        RemoveInaccessibleItems(context.Player);
    }

    private void OnPlayerHumanized(ref PlayerHumanizedContext context)
    {
        RemoveInaccessibleItems(context.Player);
    }

    private void OnPlayerBecameNemesis(ref PlayerBecameNemesisContext context)
    {
        RemoveInaccessibleItems(context.Player);
    }

    private void OnPlayerBecameSurvivor(ref PlayerBecameSurvivorContext context)
    {
        RemoveInaccessibleItems(context.Player);
    }

    private bool CanUseItemInternal(IPlayer player, ItemBase item)
    {
        if (item is IManagedGameplayItem { Enabled: false })
        {
            return false;
        }

        var playerFlag = zombiePlagueApi().IsInfected(player)
            ? AccessFlags.Zombie
            : AccessFlags.Human;

        return (item.AccessFlags & playerFlag) != 0;
    }

    private void OnMapLoad(IOnMapLoadEvent _)
    {
        core.Scheduler.NextWorldUpdate(RemoveForbiddenBombsAndLegacyCarriers);
    }

    private void RemoveForbiddenBombsAndLegacyCarriers()
    {
        if (!_initialized) return;

        var entities = core.EntitySystem.GetAllEntities()
            .Where(IsForbiddenEntityCandidate)
            .ToArray();

        foreach (var entity in entities)
        {
            RemoveIfForbidden(entity);
        }
    }

    private void RemoveIfForbidden(CEntityInstance entity)
    {
        if (!_initialized || !entity.IsValid) return;

        if (entity.DesignerName == C4DesignerName)
        {
            if (!LaserMinePolicy.ShouldRemoveC4(GetItemByIndex(entity.Index) is LaserMine))
            {
                return;
            }

            entity.Despawn();
            return;
        }

        if (entity.DesignerName == LegacyLaserMineDesignerName)
        {
            var legacyCarrier = entity.As<CCSWeaponBase>();

            if (legacyCarrier?.AttributeManager.Item.CustomName != LaserMine.ItemDisplayName)
            {
                return;
            }
        }

        entity.Despawn();
    }

    private static bool IsForbiddenEntityCandidate(CEntityInstance entity)
    {
        return entity.DesignerName is C4DesignerName or
            PlantedC4DesignerName or
            LegacyLaserMineDesignerName;
    }

    private static void DenyWeaponUse(ref CanUseWeaponPreContext context)
    {
        context.SetReturn(false);
        context.SetHookResult(HookResult.Stop);
    }
}
