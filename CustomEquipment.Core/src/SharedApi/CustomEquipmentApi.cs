using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Services;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using EquipmentGiveAction = CustomEquipment.Data.Equipments.Enums.GiveAction;
using EquipmentSlotContract = CustomEquipment.Data.Equipments.Enums.Slot;
using EquipmentTypeContract = CustomEquipment.Data.Equipments.Enums.WeaponType;

namespace CustomEquipment.SharedApi;

internal sealed class CustomEquipmentApi(
    ISwiftlyCore core,
    IItemService itemService,
    IEquipmentService equipmentService
) : ICustomEquipmentApi
{
    public IReadOnlyCollection<EquipmentItem> GetItems()
    {
        return itemService
            .GetAllRegisteredItems()
            .Select(Map)
            .OrderBy(item => item.Category)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool TryGetItem(string itemId, out EquipmentItem? item)
    {
        item = null;

        if (string.IsNullOrWhiteSpace(itemId) || !itemService.TryGet(itemId, out var registeredItem))
        {
            return false;
        }

        item = Map(registeredItem);
        return true;
    }

    public EquipmentGiveResult GiveItem(
        IPlayer player,
        string itemId,
        EquipmentGiveMode mode = EquipmentGiveMode.DropExisting
    )
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.IsValid || !player.IsAlive || player.PlayerPawn is not { IsValid: true })
        {
            return EquipmentGiveResult.InvalidPlayer;
        }

        if (string.IsNullOrWhiteSpace(itemId) || !itemService.TryGet(itemId, out _))
        {
            return EquipmentGiveResult.ItemNotFound;
        }

        var action = mode switch
        {
            EquipmentGiveMode.DropExisting => EquipmentGiveAction.Drop,
            EquipmentGiveMode.RemoveExisting => EquipmentGiveAction.Remove,
            _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, null)
        };

        try
        {
            return equipmentService.GiveItem(player, itemId, action)
                ? EquipmentGiveResult.Success
                : EquipmentGiveResult.DeliveryFailed;
        }
        catch (Exception exception)
        {
            core.Logger.LogError(
                exception,
                "Could not give CustomEquipment item {ItemId} to player {PlayerId}.",
                itemId,
                player.PlayerID
            );

            return EquipmentGiveResult.DeliveryFailed;
        }
    }

    private static EquipmentItem Map(IItem item)
    {
        return new EquipmentItem(
            Id: item.InternalName,
            DisplayName: item.DisplayName,
            Category: MapCategory(item),
            Slot: MapSlot(item.Slot)
        );
    }

    private static EquipmentCategory MapCategory(IItem item)
    {
        var type = item switch
        {
            IWeapon weapon => weapon.WeaponType,
            IGrenade grenade => grenade.WeaponType,
            BaseEquipment equipment => equipment.WeaponType,
            _ => throw new InvalidOperationException(
                $"Registered item '{item.InternalName}' has no supported equipment category."
            )
        };

        return type switch
        {
            EquipmentTypeContract.Pistol => EquipmentCategory.Pistol,
            EquipmentTypeContract.SubmachineGun => EquipmentCategory.SubmachineGun,
            EquipmentTypeContract.Rifle => EquipmentCategory.Rifle,
            EquipmentTypeContract.Shotgun => EquipmentCategory.Shotgun,
            EquipmentTypeContract.SniperRifle => EquipmentCategory.SniperRifle,
            EquipmentTypeContract.MachineGun => EquipmentCategory.MachineGun,
            EquipmentTypeContract.Grenade => EquipmentCategory.Grenade,
            EquipmentTypeContract.Equipment => EquipmentCategory.Equipment,
            _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
        };
    }

    private static EquipmentSlot MapSlot(EquipmentSlotContract slot)
    {
        return slot switch
        {
            EquipmentSlotContract.Primary => EquipmentSlot.Primary,
            EquipmentSlotContract.Secondary => EquipmentSlot.Secondary,
            EquipmentSlotContract.Knife => EquipmentSlot.Knife,
            EquipmentSlotContract.Grenade => EquipmentSlot.Grenade,
            EquipmentSlotContract.Equipment => EquipmentSlot.Equipment,
            _ => throw new ArgumentOutOfRangeException(nameof(slot), slot, null)
        };
    }
}
