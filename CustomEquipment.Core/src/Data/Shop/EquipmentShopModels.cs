namespace CustomEquipment.Data.Shop;

internal enum EquipmentShopType
{
    Human,
    Zombie
}

internal interface IEquipmentShopListingSettings
{
}

internal sealed record EmptyEquipmentShopListingSettings : IEquipmentShopListingSettings
{
    public static readonly EmptyEquipmentShopListingSettings Instance = new();
}

internal sealed record ArmorEquipmentShopListingSettings(
    int ArmorAmount
) : IEquipmentShopListingSettings;

internal sealed record EquipmentShopSettingsDefinition(
    EquipmentShopType ShopType,
    string DisplayName,
    string DisplayNameKey,
    bool Enabled,
    int MaxPurchasesPerRound,
    int MaxPurchasesPerMap
);

internal sealed record EquipmentShopCategoryDefinition(
    long Id,
    EquipmentShopType ShopType,
    string Key,
    string DisplayName,
    string DisplayNameKey,
    string Description,
    string? DescriptionKey,
    bool Enabled,
    int SortOrder
);

internal sealed record EquipmentShopListingDefinition(
    long Id,
    EquipmentShopType ShopType,
    string ItemInternalName,
    long CategoryId,
    string Description,
    string? DescriptionKey,
    int Price,
    int MaxPurchasesPerRound,
    int MaxPurchasesPerMap,
    bool Enabled,
    int SortOrder,
    IEquipmentShopListingSettings Settings
);

internal sealed record EquipmentShopRoleLimitDefinition(
    long Id,
    EquipmentShopType ShopType,
    string PrivilegeKey,
    int MaxPurchasesPerRound,
    int MaxPurchasesPerMap,
    bool Enabled,
    int SortOrder
);

internal sealed record EquipmentShopProductDefinition(
    string ImplementationKey,
    string InternalName,
    string DisplayName,
    string DisplayNameKey,
    bool Enabled,
    int SortOrder
);

internal sealed record EquipmentShopSnapshot(
    IReadOnlyDictionary<EquipmentShopType, EquipmentShopSettingsDefinition> Settings,
    IReadOnlyCollection<EquipmentShopCategoryDefinition> Categories,
    IReadOnlyCollection<EquipmentShopListingDefinition> Listings,
    IReadOnlyCollection<EquipmentShopRoleLimitDefinition> RoleLimits,
    IReadOnlyDictionary<string, EquipmentShopProductDefinition> Products
);
