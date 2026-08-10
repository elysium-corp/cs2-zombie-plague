using CustomEquipment.Api;
using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Data.Equipments.Weapons.Grenades;
using CustomEquipment.Giver;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Services;

internal sealed class EquipmentService(
    ISwiftlyCore core,
    IItemGiver itemGiver,
    IEventPublisher eventPublisher,
    IEventSubscriber eventSubscriber
) : IEquipmentService, IDisposable
{
    private readonly List<BaseItem> _items = [];
    private readonly Dictionary<IPlayer, HashSet<BaseWeapon>> _inventories = [];
    private Guid _playerDisconnectHook = Guid.Empty;
    private bool _isActive;

    public void Initialize()
    {
        _isActive = true;

        core.Event.OnEntityCreated += OnEntityCreated;
        core.Event.OnEntityDeleted += OnEntityDeleted;
        core.Event.OnWeaponServicesCanUseHook += OnWeaponServicesCanUseHook;
        core.Event.OnWeaponServicesDropWeaponHook += OnWeaponServicesDropWeaponHook;

        _playerDisconnectHook = core.GameEvent.HookPost<EventPlayerDisconnect>(OnPlayerDisconnect);

        eventSubscriber.OnGrenadeThrown += OnGrenadeThrown;
    }
    
    public void Dispose()
    {
        _isActive = false;

        core.Event.OnEntityCreated -= OnEntityCreated;
        core.Event.OnEntityDeleted -= OnEntityDeleted;
        core.Event.OnWeaponServicesCanUseHook -= OnWeaponServicesCanUseHook;
        core.Event.OnWeaponServicesDropWeaponHook -= OnWeaponServicesDropWeaponHook;

        core.GameEvent.Unhook(_playerDisconnectHook);
        
        eventSubscriber.OnGrenadeThrown -= OnGrenadeThrown;

        _items.Clear();
        _inventories.Clear();
    }

    private void OnGrenadeThrown(IGrenade grenade, CBaseCSGrenadeProjectile projectile)
    {
        projectile.SetModel(grenade.Model);
    }

    private HookResult OnPlayerDisconnect(EventPlayerDisconnect @event)
    {
        if (@event.UserIdPlayer is { } player)
        {
            _inventories.Remove(player);
        }

        return HookResult.Continue;
    }

    public List<BaseWeapon> GetAllWeapons() => _items.OfType<BaseWeapon>().ToList();
    
    public List<BaseGrenade> GetAllGrenades() => _items.OfType<BaseGrenade>().ToList();

    public bool GiveItem(IPlayer player, string itemId, GiveAction action = GiveAction.Drop)
    {
        var item = itemGiver.GiveItem(player, itemId, action);

        if (item is BaseWeapon or BaseGrenade)
        {
            AddOrReplace(item);
        }

        return item is not null;
    }
    
    public BaseWeapon? GiveWeapon<TWeapon>(IPlayer player) where TWeapon : BaseWeapon
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

        return _items.Find(item => item.AttachedEntity.Index == activeWeaponIndex) as TItem;
    }

    public TWeapon? GetActiveWeapon<TWeapon>(IPlayer player) where TWeapon : BaseWeapon
    {
        var activeWeaponIndex = player.RequiredPawn.WeaponServices?.ActiveWeapon.Value?.Index;

        if (activeWeaponIndex == null) return null;

        return _items.Find(item => item.AttachedEntity.Index == activeWeaponIndex) as TWeapon;
    }

    private void OnEntityCreated(IOnEntityCreatedEvent hook)
    {
        var entity = hook.Entity;
        
        if (!entity.IsValid || entity is not CBaseCSGrenadeProjectile) return;
        
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (!_isActive || !entity.IsValid)
            {
                return;
            }

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
            if (!_isActive
                || !_inventories.TryGetValue(player, out var currentInventory)
                || !ReferenceEquals(currentInventory, inventory))
            {
                return;
            }

            inventory.RemoveWhere(weapon =>
                !weaponsInInventoryAsIds.Contains((int)weapon.AttachedWeapon.Index)
            );
        });
    }

    private void AddWeaponToInventory(IPlayer player, BaseWeapon weapon)
    {
        if (!_inventories.TryGetValue(player, out var weapons))
        {
            weapons = [];
            _inventories[player] = weapons;
        }

        weapons.Add(weapon);
    }

    private BaseWeapon? GetWeaponByIndex(uint index)
    {
        return GetAllWeapons().Find(wp => wp.AttachedEntity.Index == index);
    }

    private BaseGrenade? GetGrenadeByIndex(uint index)
    {
        return GetAllGrenades().Find(wp => wp.AttachedEntity.Index == index);
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
        _items[index] = item;
        return item;
    }
}
