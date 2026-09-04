using System.Text.Json;
using CustomEquipment.Api;
using Shop.Api.Data;
using Shop.Core.Data;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Application;

internal sealed class ShopProductProvider(Func<ICustomEquipmentApi> equipmentApi)
{
    private const string CustomEquipmentProvider = "custom_equipment";
    private const string BuiltinProvider = "builtin";
    private const string ArmorItem = "armor";

    public bool IsAvailable(IPlayer player, ShopOfferDefinition offer)
    {
        return offer.Contract.ProviderKey switch
        {
            CustomEquipmentProvider =>
                equipmentApi().TryGetRegisteredItem(offer.Contract.ItemKey, out _) &&
                equipmentApi().CanUseItem(player, offer.Contract.ItemKey),
            BuiltinProvider when offer.Contract.ItemKey == ArmorItem =>
                player.PlayerPawn is { IsValid: true },
            _ => false
        };
    }

    public bool TryGrant(IPlayer player, ShopOfferDefinition offer)
    {
        return offer.Contract.ProviderKey switch
        {
            CustomEquipmentProvider =>
                equipmentApi().TryGiveItem(player, offer.Contract.ItemKey),
            BuiltinProvider when offer.Contract.ItemKey == ArmorItem =>
                TryGrantArmor(player, offer.SettingsJson),
            _ => false
        };
    }

    private static bool TryGrantArmor(IPlayer player, string settingsJson)
    {
        if (player.PlayerPawn is not { IsValid: true } pawn)
        {
            return false;
        }

        using var document = JsonDocument.Parse(settingsJson);
        var root = document.RootElement;
        var amount = root.TryGetProperty("armor_amount", out var snakeCase)
            ? snakeCase.GetInt32()
            : root.TryGetProperty("armorAmount", out var camelCase)
                ? camelCase.GetInt32()
                : 100;
        amount = Math.Clamp(amount, 1, 100);
        var current = Math.Clamp(pawn.ArmorValue, 0, 100);
        var updated = Math.Clamp(current + amount, 0, 100);
        if (updated == current)
        {
            return false;
        }

        pawn.ArmorValue = updated;
        pawn.ArmorValueUpdated();
        return true;
    }
}
