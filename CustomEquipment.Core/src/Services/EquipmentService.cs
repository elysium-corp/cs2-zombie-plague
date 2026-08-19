using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events;
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
using IEventSubscriber = CustomEquipment.Api.Events.IEventSubscriber;

namespace CustomEquipment.Services;

internal sealed class EquipmentService(
    ISwiftlyCore core,
    IItemGiver itemGiver,
    IItemRegistry itemRegistry,
    IEventPublisher eventPublisher,
    IEventSubscriber eventSubscriber,
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

        eventSubscriber.OnGrenadeThrown += OnGrenadeThrown;
    }

    public void Dispose()
    {
        core.Event.OnEntityCreated -= OnEntityCreated;
        core.Event.OnEntityDeleted -= OnEntityDeleted;

        core.GameHooks.Weapons.CanUse.Pre -= OnWeaponCanUsePre;
        core.GameHooks.Weapons.CanUse.Post -= OnWeaponCanUsePost;
        core.GameHooks.Weapons.Drop.Post -= OnWeaponDropPost;

        eventSubscriber.OnGrenadeThrown -= OnGrenadeThrown;
    }

    private void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile)
    {
        projectile.SetModel(grenade.Model);
    }

    public IEnumerable<ItemBase> GetAllItems() => _items;

    public IEnumerable<WeaponItemBase> GetAllWeapons() => _items.OfType<WeaponItemBase>();

    public IEnumerable<GrenadeItemBase> GetAllGrenades() => _items.OfType<GrenadeItemBase>();

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
    
    private void OnItemGiven(IPlayer player, ItemBase item)
    {
        switch (item)
        {
            case WeaponItemBase weapon:
                AddOrReplace(weapon);
                eventPublisher.OnWeaponGiven(player, weapon);
                break;

            case GrenadeItemBase grenade:
                AddOrReplace(grenade);
                eventPublisher.OnGrenadeGiven(player, grenade);
                break;

            case EquipmentItemBase:
                break;
        }

        eventPublisher.OnItemGiven(player, item);
    }

    public void GiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!CanUseItem(player, internalName))
        {
            return;
        }

        var createdItem = itemRegistry.Create(internalName);

        if (createdItem is not ItemBase item)
        {
            throw new InvalidOperationException($"Item '{internalName}' is not an ItemBase!");
        }

        itemGiver.GiveItem(player, item, action, completedItem => { OnItemGiven(player, completedItem); });
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

    private void OnEntityCreated(IOnEntityCreatedEvent hook)
    {
        var entity = hook.Entity;

        if (!entity.IsValid || entity is not CBaseCSGrenadeProjectile) return;

        core.Scheduler.NextWorldUpdate(() =>
        {
            var projectile = entity.As<CBaseCSGrenadeProjectile>();

            var grenade = ResolveGrenadeByProjectile(projectile);

            if (grenade == null) return;

            eventPublisher.OnGrenadeThrown(grenade, projectile);
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

        if (customWeapon is null)
        {
            return;
        }

        if (CanUseItem(context.Params.Player, customWeapon))
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
        return GetAllGrenades().ToList().Find(wp => wp.AttachedEntity.Index == index);
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