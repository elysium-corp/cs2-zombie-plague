using CustomEquipment.Data.Shop;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal enum EquipmentShopPurchaseLimitReason
{
    None,
    ShopDisabled,
    RoundLimitReached,
    MapLimitReached,
    ItemRoundLimitReached,
    ItemMapLimitReached
}

internal readonly record struct EquipmentShopPurchaseAvailability(
    bool Allowed,
    EquipmentShopPurchaseLimitReason Reason
);

internal interface IEquipmentShopPurchaseLimitService
{
    void Initialize();

    EquipmentShopPurchaseAvailability CanPurchase(
        IPlayer player,
        EquipmentShopListingDefinition listing
    );

    void RecordPurchase(
        IPlayer player,
        EquipmentShopListingDefinition listing
    );
}
