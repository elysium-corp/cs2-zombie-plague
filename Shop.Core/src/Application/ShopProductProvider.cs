using System.Text.Json;
using CustomEquipment.Api;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using Shop.Api.Data;
using Shop.Core.Data;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Shop.Core.Application;

internal sealed class ShopProductProvider(Func<ICustomEquipmentApi> equipmentApi)
{
    private const string CustomEquipmentProvider = "custom_equipment";
    private const string BuiltinProvider = "builtin";
    private const string ArmorItem = "armor";

    public ItemRarity? GetRarity(ShopOfferDefinition offer)
    {
        if (offer.Contract.ProviderKey == StandardWeaponCatalog.ProviderKey)
        {
            return StandardWeaponCatalog.Weapons.ContainsKey(offer.Contract.ItemKey)
                ? ItemRarity.Common
                : null;
        }

        if (offer.Contract.ProviderKey != CustomEquipmentProvider ||
            !equipmentApi().TryGetRegisteredItem(offer.Contract.ItemKey, out var item))
        {
            return null;
        }

        return item switch
        {
            IHasRarity rarityItem => rarityItem.Rarity,
            IShopItem legacyShopItem => legacyShopItem.Rarity,
            _ => null
        };
    }

    public bool IsAvailable(IPlayer player, ShopOfferDefinition offer)
    {
        return offer.Contract.ProviderKey switch
        {
            CustomEquipmentProvider =>
                equipmentApi().TryGetRegisteredItem(offer.Contract.ItemKey, out _) &&
                equipmentApi().CanUseItem(player, offer.Contract.ItemKey),
            BuiltinProvider when offer.Contract.ItemKey == ArmorItem =>
                player.PlayerPawn is { IsValid: true },
            StandardWeaponCatalog.ProviderKey =>
                IsStandardWeaponAvailable(player, offer),
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
            StandardWeaponCatalog.ProviderKey =>
                TryGrantStandardWeapon(player, offer),
            _ => false
        };
    }

    private static bool IsStandardWeaponAvailable(IPlayer player, ShopOfferDefinition offer) =>
        offer.ShopType == ShopType.Human &&
        StandardWeaponCatalog.Weapons.ContainsKey(offer.Contract.ItemKey) &&
        player.IsValid && player.IsAlive &&
        player.PlayerPawn is { IsValid: true, ItemServices: not null, WeaponServices: not null };

    private static bool TryGrantStandardWeapon(IPlayer player, ShopOfferDefinition offer)
    {
        if (!IsStandardWeaponAvailable(player, offer))
        {
            return false;
        }

        var pawn = player.RequiredPlayerPawn;
        var weapons = pawn.WeaponServices!;
        weapons.DropWeaponBySlot(StandardWeaponCatalog.Weapons[offer.Contract.ItemKey]);

        // Используем возвращённую сущность: поиск по имени мог бы принять старое оружие за покупку.
        var weapon = pawn.ItemServices!.GiveItem<CCSWeaponBase>(offer.Contract.ItemKey);
        return weapon is { IsValid: true };
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
