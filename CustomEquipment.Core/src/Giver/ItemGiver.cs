using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events;
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
    public void GiveItem(IPlayer player, ItemBase item, GiveAction action, Action<ItemBase> onCompleted)
    {
        switch (item)
        {
            case WeaponItemBase weapon:
                GiveWeapon(player, weapon, action, onCompleted);
                break;

            case GrenadeItemBase grenade:
                GiveGrenade(player, grenade, action);
                break;

            case EquipmentItemBase equipment:
                GiveEquipment(player, equipment, onCompleted);
                break;

            default:
                throw new NotSupportedException($"Item type '{item.GetType().FullName}' is not supported!");
        }
    }

    private void GiveWeapon(IPlayer player, WeaponItemBase weapon, GiveAction action, Action<ItemBase> onCompleted)
    {
        var pawn = player.RequiredPlayerPawn;
        var itemServices = pawn.ItemServices;
        var weaponServices = pawn.WeaponServices;

        if (itemServices == null ||
            weaponServices == null)
        {
            return;
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
        }

        var name = ResolveInheritorName(weapon.InheritorName);

        var originalWeapon = CreateOriginalWeapon(itemServices, weaponServices, name);

        if (originalWeapon == null)
        {
            return;
        }

        weapon.AttachedWeapon = originalWeapon;

        onCompleted(weapon);
    }

    private void GiveGrenade(IPlayer player, GrenadeItemBase grenade, GiveAction action, Action<ItemBase> onCompleted)
    {
        var pawn = player.RequiredPlayerPawn;
        var itemServices = pawn.ItemServices;
        var weaponServices = pawn.WeaponServices;

        if (itemServices == null || weaponServices == null)
        {
            return;
        }

        var name = ResolveInheritorName(grenade.InheritorName);

        switch (action)
        {
            case GiveAction.Drop:
                weaponServices.DropWeaponByDesignerName(name);
                break;

            case GiveAction.Remove:
                weaponServices.RemoveWeaponByDesignerName(name);
                break;
        }

        var weaponsBefore = weaponServices.MyWeaponsAsIds();

        // Именно string API
        itemServices.GiveItem(name);

        core.Scheduler.NextWorldUpdate(() =>
        {
            if (!player.IsValid)
            {
                return;
            }

            var currentWeaponServices = player.PlayerPawn?.WeaponServices;

            var originalGrenade = currentWeaponServices?.MyValidWeapons
                .FirstOrDefault(weapon =>
                    weapon.DesignerName == name &&
                    !weaponsBefore.Contains(
                        (int)weapon.Index
                    )
                )
                ?.As<CBaseCSGrenade>();

            if (originalGrenade == null)
            {
                return;
            }

            grenade.AttachedGrenade = originalGrenade;

            onCompleted(grenade);
        });
    }
}