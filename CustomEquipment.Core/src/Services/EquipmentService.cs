using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Giver;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using IEventSubscriber = CustomEquipment.Api.Events.IEventSubscriber;

namespace CustomEquipment.Services;

internal sealed class EquipmentService(ISwiftlyCore core, IItemGiver itemGiver, IEventPublisher eventPublisher, IEventSubscriber eventSubscriber) : IEquipmentService, IDisposable
{
    private readonly List<ItemBase> _items = [];
    private readonly Dictionary<IPlayer, HashSet<WeaponItemBase>> _inventories = [];

    public void Initialize()
    {
        core.Event.OnEntityCreated += OnEntityCreated;
        core.Event.OnEntityDeleted += OnEntityDeleted;

        core.GameHooks.Weapons.CanUse.Post += OnWeaponCanUsePost;
        core.GameHooks.Weapons.Drop.Post += OnWeaponDropPost;

        eventSubscriber.OnGrenadeThrown += OnGrenadeThrown;
    }

    public void Dispose()
    {
        core.Event.OnEntityCreated -= OnEntityCreated;
        core.Event.OnEntityDeleted -= OnEntityDeleted;

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
    
    public TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop) where TWeapon : WeaponItemBase
    {
        var weapon = itemGiver.GiveWeapon<TWeapon>(player, action);

        if (weapon is null)
        {
            return null;
        }

        return AddOrReplace(weapon);
    }

    public WeaponItemBase? GiveWeapon(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        var weapon = itemGiver.GiveWeapon(player, internalName, action);

        if (weapon is null)
        {
            return null;
        }

        return AddOrReplace(weapon);
    }

    public GrenadeItemBase? GiveGrenade<TGrenade>(IPlayer player) where TGrenade : GrenadeItemBase
    {
        var grenade = itemGiver.GiveGrenade<TGrenade>(player);

        return grenade is null ? null : AddOrReplace(grenade);
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
}