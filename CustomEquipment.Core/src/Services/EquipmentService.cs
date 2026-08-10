using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Events;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Giver;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using IEventSubscriber = CustomEquipment.Api.Events.IEventSubscriber;

namespace CustomEquipment.Services;

internal sealed class EquipmentService(ISwiftlyCore core, IItemGiver itemGiver, IEventPublisher eventPublisher, IEventSubscriber eventSubscriber) : IEquipmentService, IDisposable
{
    private readonly List<BaseItem> _items = [];
    private readonly Dictionary<IPlayer, HashSet<WeaponBase>> _inventories = [];

    public void Initialize()
    {
        core.Event.OnEntityCreated += OnEntityCreated;
        core.Event.OnEntityDeleted += OnEntityDeleted;
        core.Event.OnWeaponServicesCanUseHook += OnWeaponServicesCanUseHook;
        core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;

        eventSubscriber.OnGrenadeThrown += OnGrenadeThrown;
    }
    
    public void Dispose()
    {
        core.Event.OnEntityCreated -= OnEntityCreated;
        core.Event.OnEntityDeleted -= OnEntityDeleted;
        core.Event.OnWeaponServicesCanUseHook -= OnWeaponServicesCanUseHook;
        core.Event.OnWeaponServicesDropWeaponHook -= OnWeaponServicesDropWeaponHook;
        
        eventSubscriber.OnGrenadeThrown -= OnGrenadeThrown;
    }

    private void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile)
    {
        projectile.SetModel(grenade.Model);
    }

    public IEnumerable<BaseItem> GetAllItems() => _items;
    
    public IEnumerable<WeaponBase> GetAllWeapons() => _items.OfType<WeaponBase>();
    
    public IEnumerable<BaseGrenade> GetAllGrenades() => _items.OfType<BaseGrenade>();
    
    public WeaponBase? GiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponBase
    {
        var weapon = itemGiver.GiveWeapon<TWeapon>(player);

        if (weapon == null) return null;
        
        AddOrReplace(weapon);
        
        return weapon;
    }

    public BaseGrenade? GiveGrenade<TGrenade>(IPlayer player) where TGrenade : BaseGrenade
    {
        var grenade = itemGiver.GiveGrenade<TGrenade>(player);
        
        if (grenade == null) return null;
        
        AddOrReplace(grenade);
        
        return grenade;
    }

    public TItem? GetActiveItem<TItem>(IPlayer player) where TItem : BaseItem
    {
        var activeWeaponIndex = player.RequiredPawn.WeaponServices?.ActiveWeapon.Value?.Index;

        if (activeWeaponIndex == null) return null;

        return _items.Find(wp => wp.AttachedEntity.Index == activeWeaponIndex || wp.AttachedEntity.Index == activeWeaponIndex) as TItem;
    }

    public TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : WeaponBase
    {
        var activeWeaponIndex = player.RequiredPawn.WeaponServices?.ActiveWeapon.Value?.Index;

        if (activeWeaponIndex == null) return null;

        return _items.Find(wp => wp.AttachedEntity.Index == activeWeaponIndex || wp.AttachedEntity.Index == activeWeaponIndex) as TWeapon;
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

    private void OnWeaponServicesCanUseHook(IOnWeaponServicesCanUseHookEvent hook)
    {
        var player = hook.WeaponServices.Pawn.ToPlayer();
        var engineResult = hook.OriginalResult;

        if (!engineResult || player == null) return;

        var weapon = hook.Weapon;
        var customWeapon = GetWeaponByIndex(weapon.Index);

        if (customWeapon == null) return;

        AddWeaponToInventory(player, customWeapon);
    }

    private void OnWeaponServicesDropWeaponHook(IOnWeaponServicesDropWeaponHook hook)
    {
        var weaponService = hook.WeaponServices;
        var player = weaponService.Pawn.ToPlayer();

        if (player == null) return;

        var weaponsInInventoryAsIds = weaponService.MyWeaponsAsIds();
        
        if (!_inventories.TryGetValue(player, out var inventory))
        {
            return;
        }
        
        core.Scheduler.NextWorldUpdate(() =>
        {
            foreach (var weapon in inventory)
            {
                var weaponId = weapon.AttachedWeapon.Index;

                if (!weaponsInInventoryAsIds.Contains((int)weaponId))
                {
                    inventory.Remove(weapon);
                }
            }
        });
    }

    private void AddWeaponToInventory(IPlayer player, WeaponBase weapon)
    {
        if (!_inventories.TryGetValue(player, out var weapons))
        {
            weapons = [];
            _inventories[player] = weapons;
        }

        weapons.Add(weapon);
    }

    private WeaponBase? GetWeaponByIndex(uint index)
    {
        return GetAllWeapons().ToList().Find(wp => wp.AttachedEntity.Index == index);
    }

    private BaseGrenade? GetGrenadeByIndex(uint index)
    {
        return GetAllGrenades().ToList().Find(wp => wp.AttachedEntity.Index == index);
    }

    private BaseGrenade? ResolveGrenadeByProjectile(CBaseCSGrenadeProjectile projectile)
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
    
    private BaseItem AddOrReplace(BaseItem item)
    {
        var foundItem = _items.Find(wp => wp.AttachedEntity.Index == item.AttachedEntity.Index);

        if (foundItem == null)
        {
            _items.Add(item);
            return item;
        }

        var index = _items.IndexOf(foundItem);
        _items[index] = foundItem;
        return foundItem;
    }
}