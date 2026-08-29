using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events.Contexts.Grenades;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Giver;
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
    private readonly List<ItemBase> _items = [];

    public void Initialize()
    {
        core.Event.OnEntityCreated += OnEntityCreated;
        core.Event.OnEntityDeleted += OnEntityDeleted;

        core.GameHooks.Weapons.CanUse.Pre += OnWeaponCanUsePre;

        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Hook(OnPlayerInfected);
        playerEvents.Disinfected.Hook(OnPlayerDisinfected);
        playerEvents.Humanized.Hook(OnPlayerHumanized);
        playerEvents.BecameNemesis.Hook(OnPlayerBecameNemesis);
        playerEvents.BecameSurvivor.Hook(OnPlayerBecameSurvivor);
    }

    public void Dispose()
    {
        core.Event.OnEntityCreated -= OnEntityCreated;
        core.Event.OnEntityDeleted -= OnEntityDeleted;

        core.GameHooks.Weapons.CanUse.Pre -= OnWeaponCanUsePre;

        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Unhook(OnPlayerInfected);
        playerEvents.Disinfected.Unhook(OnPlayerDisinfected);
        playerEvents.Humanized.Unhook(OnPlayerHumanized);
        playerEvents.BecameNemesis.Unhook(OnPlayerBecameNemesis);
        playerEvents.BecameSurvivor.Unhook(OnPlayerBecameSurvivor);

        _items.Clear();
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

    private void OnItemGiven(IPlayer player, ItemBase item, GiveAction action)
    {
        AddOrReplace(item);

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

        if (!CanUseItemInternal(player, item))
        {
            RemoveItem(player, item);
        }
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

        try
        {
            itemGiver.GiveItem(
                preparedPlayer,
                preparedItem,
                preparedAction,
                completedItem => OnItemGiven(preparedPlayer, completedItem, preparedAction)
            );
        }
        catch (Exception exception)
        {
            DispatchGiveFailed(preparedPlayer, internalName, preparedAction, exception);
            throw;
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

    private void OnEntityCreated(IOnEntityCreatedEvent hook)
    {
        var entity = hook.Entity;

        if (!entity.IsValid || entity is not CBaseCSGrenadeProjectile) return;

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
        var customWeapon = GetItemByIndex(weapon.Index);

        if (customWeapon is null || CanUseItem(context.Params.Player, customWeapon))
        {
            return;
        }

        context.SetReturn(false);
        context.SetHookResult(HookResult.Stop);
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
        var playerFlag = zombiePlagueApi().IsInfected(player)
            ? AccessFlags.Zombie
            : AccessFlags.Human;

        return (item.AccessFlags & playerFlag) != 0;
    }
}
