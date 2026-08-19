using CustomEquipment.Api.Data;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Weapons;
using CustomEquipment.Mappers;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Giver;

internal sealed class ItemGiver(ISwiftlyCore core) : IItemGiver
{
    public void GiveItem(IPlayer player, ItemBase item, GiveAction action, Action<ItemBase> onCompleted)
    {
        switch (item)
        {
            case WeaponItemBase weapon:
                GiveWeapon(player, weapon, action, onCompleted);
                break;

            case GrenadeItemBase grenade:
                GiveGrenade(player, grenade, action, onCompleted);
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

        if (itemServices == null || weaponServices == null)
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

        var originalWeapon = CreateOriginalWeapon(
            itemServices,
            weaponServices,
            name
        );

        if (originalWeapon == null)
        {
            return;
        }

        weapon.AttachedWeapon = originalWeapon;

        onCompleted(weapon);
    }

    private void GiveGrenade(
        IPlayer player,
        GrenadeItemBase grenade,
        GiveAction action,
        Action<ItemBase> onCompleted
    )
    {
        var pawn = player.RequiredPlayerPawn;
        var itemServices = pawn.ItemServices;
        var weaponServices = pawn.WeaponServices;

        if (itemServices == null ||
            weaponServices == null)
        {
            return;
        }

        var name = ResolveInheritorName(
            grenade.InheritorName
        );

        switch (action)
        {
            case GiveAction.Drop:
                weaponServices.DropWeaponByDesignerName(name);
                break;

            case GiveAction.Remove:
                weaponServices.RemoveWeaponByDesignerName(name);
                break;
        }

        Console.WriteLine(
            $"[GRENADE] give request: {name}"
        );

        // Именно STRING
        itemServices.GiveItem(name);

        core.Scheduler.NextWorldUpdate(() =>
        {
            Console.WriteLine(
                $"[GRENADE] resolving: {name}"
            );

            if (!player.IsValid)
            {
                Console.WriteLine(
                    $"[GRENADE] player invalid: {name}"
                );

                return;
            }

            var currentWeaponServices =
                player.PlayerPawn?.WeaponServices;

            if (currentWeaponServices == null)
            {
                Console.WriteLine(
                    $"[GRENADE] WeaponServices null: {name}"
                );

                return;
            }

            var originalGrenade =
                currentWeaponServices.MyValidWeapons
                    .FirstOrDefault(weapon =>
                        weapon.DesignerName == name
                    )
                    ?.As<CBaseCSGrenade>();

            if (originalGrenade == null)
            {
                Console.WriteLine(
                    $"[GRENADE] not found: {name}"
                );

                Console.WriteLine(
                    "[GRENADE] inventory: " +
                    string.Join(
                        ", ",
                        currentWeaponServices
                            .MyValidWeapons
                            .Select(weapon =>
                                $"{weapon.Index}:{weapon.DesignerName}"
                            )
                    )
                );

                return;
            }

            Console.WriteLine(
                $"[GRENADE] found: " +
                $"{originalGrenade.Index}:" +
                $"{originalGrenade.DesignerName}"
            );

            grenade.AttachedGrenade =
                originalGrenade;

            onCompleted(grenade);
        });
    }

    private static void GiveEquipment(IPlayer player, EquipmentItemBase equipment, Action<ItemBase> onCompleted)
    {
        equipment.OnPurchase(player);

        onCompleted(equipment);
    }

    private static CCSWeaponBase? CreateOriginalWeapon(CPlayer_ItemServices itemServices, CPlayer_WeaponServices weaponServices, string name)
    {
        if (name == $"weapon_{WeaponName.M4A1S}")
        {
            return itemServices.GiveItem<CWeaponM4A1Silencer>();
        }

        if (name == $"weapon_{WeaponName.UspS}")
        {
            return itemServices.GiveItem<CWeaponUSPSilencer>();
        }

        itemServices.GiveItem(name);

        return weaponServices.MyValidWeapons
            .FirstOrDefault(weapon => weapon.DesignerName == name)
            ?.As<CCSWeaponBase>();
    }

    private static string ResolveInheritorName(string inheritorName)
    {
        const string prefix = "weapon_";

        return inheritorName.StartsWith(prefix)
            ? inheritorName
            : $"{prefix}{inheritorName}";
    }
}