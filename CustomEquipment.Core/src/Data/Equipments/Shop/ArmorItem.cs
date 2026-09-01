using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Data.Shop;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Shop;

internal sealed class ArmorItem(
    EquipmentShopRuntimeCatalog shopCatalog,
    IEquipmentShopRoleResolver roleResolver
) : ItemBase, IShopItem, IManagedGameplayItem, IInstantEquipmentShopItem
{
    private EquipmentShopProductDefinition Product =>
        shopCatalog.GetProduct(EquipmentShopProductKeys.Armor);

    public bool Enabled => Product.Enabled;

    public int SortOrder => Product.SortOrder;

    public override AccessFlags AccessFlags => AccessFlags.All;

    public override string DisplayName => Product.DisplayName;

    public override string InternalName => Product.InternalName;

    public override string SubclassName => string.Empty;

    public override Slot Slot => Slot.Equipment;

    public override string Model => string.Empty;

    public WeaponType WeaponType => WeaponType.Equipment;

    public Price Price => new() { Item = 0 };

    public ItemRarity Rarity => ItemRarity.Common;

    public bool TryGrant(IPlayer player)
    {
        if (!player.IsValid || player.PlayerPawn is not { } pawn)
        {
            return false;
        }

        var shopType = roleResolver.GetShopType(player);

        if (!shopCatalog.TryGetListing(shopType, InternalName, out var listing) ||
            listing is not { Enabled: true, Settings: ArmorEquipmentShopListingSettings settings })
        {
            return false;
        }

        var currentArmor = Math.Clamp(pawn.ArmorValue, 0, 100);
        var updatedArmor = Math.Clamp(currentArmor + settings.ArmorAmount, 0, 100);

        if (updatedArmor == currentArmor)
        {
            return false;
        }

        pawn.ArmorValue = updatedArmor;
        pawn.ArmorValueUpdated();

        return true;
    }
}
