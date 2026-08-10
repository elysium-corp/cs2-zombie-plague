using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Api;
using CustomEquipment.Exceptions;
using CustomEquipment.Mappers;
using CustomEquipment.Services;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Giver;

internal sealed class ItemGiver(
    IItemService itemService,
    IEventPublisher eventPublisher
) : IItemGiver
{
    public BaseItem? GiveItem(IPlayer player, string itemId, GiveAction action = GiveAction.Drop)
    {
        if (!itemService.TryGet(itemId, out var prototype))
        {
            throw new NotRegisteredItemException();
        }

        return GiveItem(player, prototype, action);
    }

    public TItem? GiveItem<TItem>(IPlayer player, GiveAction action = GiveAction.Drop)
        where TItem : class, IItem
    {
        var prototype = itemService
            .GetAllRegisteredItems()
            .FirstOrDefault(item => item.GetType() == typeof(TItem));

        if (prototype is null)
        {
            throw new NotRegisteredItemException();
        }

        return GiveItem(player, prototype, action) as TItem;
    }

    public TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop)
        where TWeapon : BaseWeapon
    {
        return GiveItem<TWeapon>(player, action);
    }

    public TGrenade? GiveGrenade<TGrenade>(IPlayer player, GiveAction action = GiveAction.Drop)
        where TGrenade : BaseGrenade
    {
        return GiveItem<TGrenade>(player, action);
    }

    private BaseItem? GiveItem(IPlayer player, IItem prototype, GiveAction action)
    {
        if (prototype.Clone() is not BaseItem item)
        {
            throw new CannotCreateItemException();
        }

        var givenItem = item switch
        {
            BaseWeapon weapon => GiveWeaponInternal(player, weapon, action),
            BaseGrenade grenade => GiveGrenadeInternal(player, grenade, action),
            BaseEquipment equipment => GiveEquipmentInternal(player, equipment),
            _ => throw new ArgumentOutOfRangeException(nameof(prototype), prototype.GetType(), null)
        };

        if (givenItem is null)
        {
            return null;
        }

        switch (givenItem)
        {
            case IWeapon weapon:
                eventPublisher.OnWeaponGiven(player, weapon);
                break;
            case IGrenade grenade:
                eventPublisher.OnGrenadeGiven(player, grenade);
                break;
        }

        eventPublisher.OnItemGiven(player, givenItem);

        return givenItem;
    }

    private static BaseEquipment? GiveEquipmentInternal(IPlayer player, BaseEquipment equipment)
    {
        return equipment.TryPurchase(player) ? equipment : null;
    }

    private static BaseGrenade? GiveGrenadeInternal(
        IPlayer player,
        BaseGrenade grenade,
        GiveAction action
    )
    {
        var pawn = player.PlayerPawn;

        if (!player.IsValid || !player.IsAlive || pawn is not { IsValid: true })
        {
            return null;
        }

        var itemServices = pawn.ItemServices;
        var weaponServices = pawn.WeaponServices;

        if (itemServices is null || weaponServices is null)
        {
            return null;
        }

        var inheritorName = ResolveInheritorName(grenade.InheritorName);

        switch (action)
        {
            case GiveAction.Drop:
                weaponServices.DropWeaponByDesignerName(inheritorName);
                break;
            case GiveAction.Remove:
                weaponServices.RemoveWeaponByDesignerName(inheritorName);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }

        var originalGrenade = CreateOriginalGrenade(itemServices, weaponServices, inheritorName);
        if (originalGrenade is null)
        {
            return null;
        }

        grenade.AttachedGrenade = originalGrenade;
        return grenade;
    }

    private static BaseWeapon? GiveWeaponInternal(
        IPlayer player,
        BaseWeapon weapon,
        GiveAction action
    )
    {
        var pawn = player.PlayerPawn;

        if (!player.IsValid || !player.IsAlive || pawn is not { IsValid: true })
        {
            return null;
        }

        var itemServices = pawn.ItemServices;
        var weaponServices = pawn.WeaponServices;

        if (itemServices is null || weaponServices is null)
        {
            return null;
        }

        var slot = weapon.Slot.MapToGearSlot();

        switch (action)
        {
            case GiveAction.Drop:
                weaponServices.DropWeaponBySlot(slot);
                break;
            case GiveAction.Remove:
                weaponServices.RemoveWeaponBySlot(slot);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(action), action, null);
        }

        var inheritorName = ResolveInheritorName(weapon.InheritorName);
        var originalWeapon = CreateOriginalWeapon(itemServices, weaponServices, inheritorName);

        if (originalWeapon is null)
        {
            return null;
        }

        weapon.AttachedWeapon = originalWeapon;
        return weapon;
    }

    private static string ResolveInheritorName(string inheritorName)
    {
        const string prefix = "weapon_";

        return inheritorName.StartsWith(prefix, StringComparison.Ordinal)
            ? inheritorName
            : $"{prefix}{inheritorName}";
    }

    private static CCSWeaponBase? CreateOriginalWeapon(
        CPlayer_ItemServices itemServices,
        CPlayer_WeaponServices weaponServices,
        string name
    )
    {
        if (name.Contains(WeaponName.M4A1S, StringComparison.Ordinal))
        {
            return itemServices.GiveItem<CWeaponM4A1Silencer>();
        }

        if (name.Contains(WeaponName.UspS, StringComparison.Ordinal))
        {
            return itemServices.GiveItem<CWeaponUSPSilencer>();
        }

        itemServices.GiveItem(name);

        return weaponServices.MyValidWeapons
            .FirstOrDefault(weapon => weapon.DesignerName.Contains(name, StringComparison.Ordinal))
            ?.As<CCSWeaponBase>();
    }

    private static CBaseCSGrenade? CreateOriginalGrenade(
        CPlayer_ItemServices itemServices,
        CPlayer_WeaponServices weaponServices,
        string name
    )
    {
        itemServices.GiveItem(name);

        return weaponServices.MyValidWeapons
            .FirstOrDefault(weapon => weapon.DesignerName.Contains(name, StringComparison.Ordinal))
            ?.As<CBaseCSGrenade>();
    }
}
