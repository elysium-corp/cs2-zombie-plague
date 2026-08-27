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
    private readonly Dictionary<IPlayer, HashSet<WeaponItemBase>> _inventories = [];

    public void Initialize()
    {
        core.Event.OnEntityCreated += OnEntityCreated;
        core.Event.OnEntityDeleted += OnEntityDeleted;

        core.GameHooks.Weapons.CanUse.Pre += OnWeaponCanUsePre;
        core.GameHooks.Weapons.CanUse.Post += OnWeaponCanUsePost;
        core.GameHooks.Weapons.Drop.Post += OnWeaponDropPost;
    }

    public void Dispose()
    {
        core.Event.OnEntityCreated -= OnEntityCreated;
        core.Event.OnEntityDeleted -= OnEntityDeleted;

        core.GameHooks.Weapons.CanUse.Pre -= OnWeaponCanUsePre;
        core.GameHooks.Weapons.CanUse.Post -= OnWeaponCanUsePost;
        core.GameHooks.Weapons.Drop.Post -= OnWeaponDropPost;
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
        switch (item)
        {
            case WeaponItemBase weapon:
                AddOrReplace(weapon);
                var weaponPost = new WeaponGivePostContext(player, weapon, action);
                hooks.Dispatch(ref weaponPost);
                break;

            case GrenadeItemBase grenade:
                AddOrReplace(grenade);
                var grenadePost = new GrenadeGivePostContext(player, grenade, action);
                hooks.Dispatch(ref grenadePost);
                break;
        }

        var itemPost = new ItemGivePostContext(player, item, action);
        hooks.Dispatch(ref itemPost);
    }

    public bool TryGiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.IsValid || !CanUseItem(player, internalName))
        {
            return false;
        }

        if (itemRegistry.Create(internalName) is not ItemBase item)
        {
            throw new InvalidOperationException($"Item '{internalName}' is not an ItemBase!");
        }

        var itemPre = new ItemGivePreContext(player, item, action);

        if (!hooks.DispatchCancellable(ref itemPre) ||
            itemPre.Item is not ItemBase preparedItem ||
            !itemPre.Player.IsValid ||
            !CanUseItem(itemPre.Player, preparedItem))
        {
            return false;
        }

        var preparedPlayer = itemPre.Player;
        var preparedAction = itemPre.Action;

        switch (preparedItem)
        {
            case WeaponItemBase weapon:
            {
                var weaponPre = new WeaponGivePreContext(preparedPlayer, weapon, preparedAction);

                if (!hooks.DispatchCancellable(ref weaponPre) ||
                    weaponPre.Weapon is not WeaponItemBase preparedWeapon ||
                    !weaponPre.Player.IsValid ||
                    !CanUseItem(weaponPre.Player, preparedWeapon))
                {
                    return false;
                }

                preparedPlayer = weaponPre.Player;
                preparedItem = preparedWeapon;
                preparedAction = weaponPre.Action;
                break;
            }

            case GrenadeItemBase grenade:
            {
                var grenadePre = new GrenadeGivePreContext(preparedPlayer, grenade, preparedAction);

                if (!hooks.DispatchCancellable(ref grenadePre) ||
                    grenadePre.Grenade is not GrenadeItemBase preparedGrenade ||
                    !grenadePre.Player.IsValid ||
                    !CanUseItem(grenadePre.Player, preparedGrenade))
                {
                    return false;
                }

                preparedPlayer = grenadePre.Player;
                preparedItem = preparedGrenade;
                preparedAction = grenadePre.Action;
                break;
            }
        }

        itemGiver.GiveItem(
            preparedPlayer,
            preparedItem,
            preparedAction,
            completedItem => OnItemGiven(preparedPlayer, completedItem, preparedAction)
        );

        return true;
    }

    public TItem? GetActiveItem<TItem>(IPlayer player) where TItem : ItemBase
    {
        var activeWeaponIndex = player.RequiredPawn.WeaponServices?.ActiveWeapon.Value?.Index;

        if (activeWeaponIndex == null) return null;

        return _items.Find(wp => wp.AttachedEntity.Index == activeWeaponIndex) as TItem;
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

            var preContext = new GrenadeThrowPreContext(grenade, projectile);

            if (!hooks.DispatchCancellable(ref preContext) || !preContext.Projectile.IsValidEntity)
            {
                return;
            }

            preContext.Projectile.SetModel(preContext.Grenade.Model);

            var postContext = new GrenadeThrowPostContext(preContext.Grenade, preContext.Projectile);
            hooks.Dispatch(ref postContext);
        });
    }

    private void OnEntityDeleted(IOnEntityDeletedEvent hook)
    {
        var entity = hook.Entity;

        _items.RemoveAll(wp => wp.AttachedEntity.Index == entity.Index);
    }

    private void OnWeaponCanUsePre(ref CanUseWeaponPreContext context)
    {
        var weapon = context.Params.Weapon;
        var customWeapon = GetWeaponByIndex(weapon.Index);

        if (customWeapon is null || CanUseItem(context.Params.Player, customWeapon))
        {
            return;
        }

        context.SetReturn(false);
        context.SetHookResult(HookResult.Stop);
    }

    private void OnWeaponCanUsePost(ref CanUseWeaponPostContext context)
    {
        if (!context.Return)
        {
            return;
        }

        var player = context.Params.Player;
        var weapon = context.Params.Weapon;
        var customWeapon = GetWeaponByIndex(weapon.Index);

        if (customWeapon is null)
        {
            return;
        }

        AddWeaponToInventory(player, customWeapon);
    }

    private void OnWeaponDropPost(ref WeaponDropPostContext context)
    {
        var droppedWeapon = context.Params.Weapon;

        if (droppedWeapon is null)
        {
            return;
        }

        var player = context.Params.Player;

        if (!_inventories.TryGetValue(player, out var inventory))
        {
            return;
        }

        inventory.RemoveWhere(customWeapon => customWeapon.AttachedEntity.Index == droppedWeapon.Index);

        if (inventory.Count == 0)
        {
            _inventories.Remove(player);
        }
    }

    private void AddWeaponToInventory(IPlayer player, WeaponItemBase weaponItem)
    {
        if (!_inventories.TryGetValue(player, out var weapons))
        {
            weapons = [];
            _inventories[player] = weapons;
        }

        weapons.Add(weaponItem);
    }

    private WeaponItemBase? GetWeaponByIndex(uint index)
    {
        return _items
            .OfType<WeaponItemBase>()
            .FirstOrDefault(weapon => weapon.AttachedEntity.Index == index);
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

    private bool CanUseItemInternal(IPlayer player, ItemBase item)
    {
        var playerFlag = zombiePlagueApi().IsInfected(player)
            ? AccessFlags.Zombie
            : AccessFlags.Human;

        return (item.AccessFlags & playerFlag) != 0;
    }
}
