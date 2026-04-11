using Common.Di;
using CustomEquipment.Api;
using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Exceptions;
using CustomEquipment.Mappers;
using CustomEquipment.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Giver;

internal sealed class ItemGiver(IItemService itemService, IEventPublisher eventPublisher) : IItemGiver
{
    public TItem? GiveItem<TItem>(IPlayer player, GiveAction action = GiveAction.Drop) where TItem : class, IItem
    {
        var item = CreateItem<TItem>();

        var givenItem = item switch
        {
            BaseWeapon weapon => GiveWeapon(player, weapon, action) as TItem,
            BaseGrenade grenade => GiveGrenade(player, grenade, action) as TItem,
            _ => throw new ArgumentOutOfRangeException(nameof(TItem))
        };
        
        if (givenItem != null) eventPublisher.OnItemGiven(player, givenItem);

        return givenItem;
    }

    public TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop) where TWeapon : BaseWeapon
    {
        var weapon = CreateItem<TWeapon>();
        
        return GiveWeaponInternal(player, weapon, action);
    }
    
    private TWeapon? GiveWeapon<TWeapon>(IPlayer player, TWeapon weapon, GiveAction action = GiveAction.Drop) where TWeapon : BaseWeapon
    {
        var givenWeapon = GiveWeaponInternal(player, weapon, action);
        
        if (givenWeapon != null) eventPublisher.OnWeaponGiven(player, givenWeapon);

        return givenWeapon;
    }
    
    public TGrenade? GiveGrenade<TGrenade>(IPlayer player, GiveAction action = GiveAction.Drop) where TGrenade : BaseGrenade
    {
        var grenade = CreateItem<TGrenade>();

        return GiveGrenade(player, grenade, action);
    }
    
    private TGrenade? GiveGrenade<TGrenade>(IPlayer player, TGrenade grenade, GiveAction action) where TGrenade : BaseGrenade
    {
        var givenGrenade = GiveGrenadeInternal(player, grenade, action);
        
        if (givenGrenade != null) eventPublisher.OnGrenadeGiven(player, givenGrenade);
        
        return givenGrenade;
    }

    private TGrenade? GiveGrenadeInternal<TGrenade>(IPlayer player, TGrenade grenade, GiveAction action = GiveAction.Drop) where TGrenade : BaseGrenade
    {
        var pawn = player.RequiredPlayerPawn;
        var itemServices = pawn.ItemServices;
        var weaponService = pawn.WeaponServices;
        
        if (itemServices == null || weaponService == null) return null;
        
        var inheritorName = grenade.InheritorName;
        var resolvedInheritorName = ResolveInheritorName(inheritorName);
        
        switch (action)
        {
            case GiveAction.Drop:
                weaponService.DropWeaponByDesignerName(resolvedInheritorName);
                break;
            
            case GiveAction.Remove:
                weaponService.RemoveWeaponByDesignerName(resolvedInheritorName);
                break;
        }
        
        var originalGrenade = CreateOriginalGrenade(itemServices, weaponService, resolvedInheritorName);
        
        if (originalGrenade == null) return null;

        grenade.AttachedGrenade = originalGrenade;

        return grenade;
    }
    
    private TWeapon? GiveWeaponInternal<TWeapon>(IPlayer player, TWeapon weapon, GiveAction action) where TWeapon : BaseWeapon
    {
        var pawn = player.RequiredPlayerPawn;
        var itemServices = pawn.ItemServices;
        var weaponService = pawn.WeaponServices;
        
        if (itemServices == null || weaponService == null) return null;

        var slot = weapon.Slot.MapToGearSlot();

        switch (action)
        {
            case GiveAction.Drop:
                weaponService.DropWeaponBySlot(slot);
                break;
            
            case GiveAction.Remove:
                weaponService.RemoveWeaponBySlot(slot);
                break;
        }

        var inheritorName = weapon.InheritorName;
        var resolvedInheritorName = ResolveInheritorName(inheritorName);
        
        var originalWeapon = CreateOriginalWeapon(itemServices, weaponService, resolvedInheritorName);

        if (originalWeapon == null) return null;

        weapon.AttachedWeapon = originalWeapon;

        return weapon;
    }

    private TItem CreateItem<TItem>() where TItem : IItem
    {
        if (!itemService.HasRegistered<TItem>()) throw new NotRegisteredItemException();

        var allRegisteredWeapons = itemService.GetAllRegisteredItems();
        var item = (TItem?)allRegisteredWeapons.FirstOrDefault(wp => wp is TItem)?.Clone() 
                   ?? throw new CannotCreateItemException();

        return item;
    }
    
    private string ResolveInheritorName(string inheritorName)
    {
        const string prefix = "weapon_";
        
        if (inheritorName.Contains(prefix))
        {
            return inheritorName;
        }

        return $"{prefix}{inheritorName}";
    }

    private CCSWeaponBase? CreateOriginalWeapon(CPlayer_ItemServices itemServices, CPlayer_WeaponServices weaponService, string name)
    {
        if (name.Contains(WeaponName.M4A1S))
        {
            return itemServices.GiveItem<CWeaponM4A1Silencer>();
        }

        if (name.Contains(WeaponName.UspS))
        {
            return itemServices.GiveItem<CWeaponUSPSilencer>();
        }
        
        itemServices.GiveItem(name);
        
        return weaponService.MyValidWeapons
            .FirstOrDefault(w => w.DesignerName.Contains(name))
            ?.As<CCSWeaponBase>();
    }

    private CBaseCSGrenade? CreateOriginalGrenade(CPlayer_ItemServices itemServices, CPlayer_WeaponServices weaponService, string name)
    {
        itemServices.GiveItem(name);
        
        return weaponService.MyValidWeapons
            .FirstOrDefault(w => w.DesignerName.Contains(name))
            ?.As<CBaseCSGrenade>();
    }
}