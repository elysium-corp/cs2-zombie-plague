using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events;
using CustomEquipment.Api.Exceptions;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Mappers;
using CustomEquipment.Registry;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Giver;

internal sealed class ItemGiver(
    ISwiftlyCore core,
    IItemRegistry itemRegistry, 
    IEventPublisher eventPublisher
) : IItemGiver
{
    public TItem? GiveItem<TItem>(IPlayer player, GiveAction action = GiveAction.Drop) where TItem : class, IItem
    {
        var item = itemRegistry.Create<TItem>();

        return GiveCreatedItem(player, item, action) as TItem;
    }
    
    public ItemBase? GiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        var item = itemRegistry.Create(internalName);

        if (item is not ItemBase itemBase)
        {
            return null;
        }

        return GiveCreatedItem(player, itemBase, action) as ItemBase;
    }
    
    public WeaponItemBase? GiveWeapon(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        var item = itemRegistry.Create(internalName);

        if (item is not WeaponItemBase weapon)
        {
            throw new CannotCreateItemException($"Registered item '{internalName}' is not a weapon!");
        }

        return GiveCreatedItem(player, weapon, action) as WeaponItemBase;
    }

    public TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop) where TWeapon : WeaponItemBase
    {
        return GiveItem<TWeapon>(player, action);
    }
    
    public TGrenade? GiveGrenade<TGrenade>(IPlayer player, GiveAction action = GiveAction.Drop) where TGrenade : GrenadeItemBase
    {
        return GiveItem<TGrenade>(player, action);
    }

    private GrenadeItemBase GiveGrenadeInternal(
        IPlayer player,
        GrenadeItemBase grenade,
        GiveAction action = GiveAction.Drop
    )
    {
        var pawn = player.RequiredPlayerPawn;
        var itemServices = pawn.ItemServices;
        var weaponService = pawn.WeaponServices;

        if (itemServices == null || weaponService == null)
        {
            return grenade;
        }

        var name = ResolveInheritorName(
            grenade.InheritorName
        );

        switch (action)
        {
            case GiveAction.Drop:
                weaponService.DropWeaponByDesignerName(name);
                break;

            case GiveAction.Remove:
                weaponService.RemoveWeaponByDesignerName(name);
                break;
        }

        var weaponsBefore = weaponService.MyWeaponsAsIds();

        itemServices.GiveItem(name);

        core.Scheduler.NextWorldUpdate(() =>
        {
            if (!player.IsValid)
            {
                return;
            }

            var currentWeaponService = player.PlayerPawn?.WeaponServices;

            if (currentWeaponService == null)
            {
                return;
            }

            var originalGrenade = currentWeaponService.MyValidWeapons
                    .FirstOrDefault(weapon => weapon.DesignerName == name && !weaponsBefore.Contains((int)weapon.Index))
                    ?.As<CBaseCSGrenade>();

            if (originalGrenade == null)
            {
                return;
            }

            grenade.AttachedGrenade = originalGrenade;

            eventPublisher.OnGrenadeGiven(player, grenade);

            eventPublisher.OnItemGiven(player, grenade);
        });

        return grenade;
    }
    
    private WeaponItemBase? GiveWeaponInternal(IPlayer player, WeaponItemBase weapon, GiveAction action)
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
    
    private IItem? GiveCreatedItem(IPlayer player, IItem item, GiveAction action)
    {
        ItemBase? givenItem = item switch
        {
            WeaponItemBase weapon => GiveWeaponInternal(player, weapon, action),

            GrenadeItemBase grenade => GiveGrenadeInternal(player, grenade, action),

            _ => throw new NotSupportedException($"Giving item type '{item.GetType().FullName}' is not supported.")
        };

        switch (givenItem)
        {
            case WeaponItemBase weapon:
                eventPublisher.OnWeaponGiven(player, weapon);
                break;

            case GrenadeItemBase grenade:
                eventPublisher.OnGrenadeGiven(player, grenade);
                break;
        }

        if (givenItem is not null)
        {
            eventPublisher.OnItemGiven(player, givenItem);
        }

        return givenItem;
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

    private CCSWeaponBase? CreateOriginalWeapon(
        CPlayer_ItemServices itemServices,
        CPlayer_WeaponServices weaponService,
        string name
    )
    {
        var weaponsBefore = weaponService.MyValidWeapons
            .Select(weapon => (int)weapon.Index)
            .ToHashSet();

        if (name.Contains(WeaponName.M4A1S))
        {
            itemServices.GiveItem<CWeaponM4A1Silencer>();
        }
        else if (name.Contains(WeaponName.UspS))
        {
            itemServices.GiveItem<CWeaponUSPSilencer>();
        }
        else
        {
            itemServices.GiveItem(name);
        }

        return weaponService.MyValidWeapons
            .FirstOrDefault(
                weapon =>
                    !weaponsBefore.Contains((int)weapon.Index) &&
                    weapon.DesignerName.Contains(name)
            )
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