using System.Diagnostics.CodeAnalysis;

namespace CustomEquipment.Data.Shop;

internal sealed class EquipmentShopRuntimeCatalog
{
    private EquipmentShopSnapshot _snapshot = EquipmentShopDefaults.CreateSnapshot();

    public EquipmentShopSettingsDefinition GetSettings(EquipmentShopType shopType)
    {
        var snapshot = Volatile.Read(ref _snapshot);

        return snapshot.Settings.TryGetValue(shopType, out var settings)
            ? settings
            : throw new InvalidOperationException($"Equipment shop '{shopType}' is not configured.");
    }

    public IReadOnlyCollection<EquipmentShopCategoryDefinition> GetCategories(
        EquipmentShopType shopType,
        bool enabledOnly = true
    )
    {
        return Volatile.Read(ref _snapshot).Categories
            .Where(category =>
                category.ShopType == shopType &&
                (!enabledOnly || category.Enabled)
            )
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .ToArray();
    }

    public IReadOnlyCollection<EquipmentShopListingDefinition> GetListings(
        EquipmentShopType shopType,
        bool enabledOnly = true
    )
    {
        return Volatile.Read(ref _snapshot).Listings
            .Where(listing =>
                listing.ShopType == shopType &&
                (!enabledOnly || listing.Enabled)
            )
            .OrderBy(listing => listing.SortOrder)
            .ThenBy(listing => listing.Id)
            .ToArray();
    }

    public IReadOnlyCollection<EquipmentShopRoleLimitDefinition> GetRoleLimits(
        EquipmentShopType shopType
    )
    {
        return Volatile.Read(ref _snapshot).RoleLimits
            .Where(limit => limit.ShopType == shopType && limit.Enabled)
            .OrderBy(limit => limit.SortOrder)
            .ThenBy(limit => limit.Id)
            .ToArray();
    }

    public bool TryGetCategory(
        EquipmentShopType shopType,
        long categoryId,
        [NotNullWhen(true)] out EquipmentShopCategoryDefinition? category
    )
    {
        category = Volatile.Read(ref _snapshot).Categories.FirstOrDefault(candidate =>
            candidate.ShopType == shopType &&
            candidate.Id == categoryId
        );

        return category is not null;
    }

    public bool TryGetListing(
        EquipmentShopType shopType,
        string internalName,
        [NotNullWhen(true)] out EquipmentShopListingDefinition? listing
    )
    {
        listing = Volatile.Read(ref _snapshot).Listings.FirstOrDefault(candidate =>
            candidate.ShopType == shopType &&
            candidate.ItemInternalName.Equals(internalName, StringComparison.OrdinalIgnoreCase)
        );

        return listing is not null;
    }

    public bool TryGetListing(
        EquipmentShopType shopType,
        long listingId,
        [NotNullWhen(true)] out EquipmentShopListingDefinition? listing
    )
    {
        listing = Volatile.Read(ref _snapshot).Listings.FirstOrDefault(candidate =>
            candidate.ShopType == shopType &&
            candidate.Id == listingId
        );

        return listing is not null;
    }

    public EquipmentShopProductDefinition GetProduct(string implementationKey)
    {
        var snapshot = Volatile.Read(ref _snapshot);

        return snapshot.Products.TryGetValue(implementationKey, out var product)
            ? product
            : throw new InvalidOperationException(
                $"Equipment shop product '{implementationKey}' is not configured."
            );
    }

    public void Replace(EquipmentShopSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        foreach (var shopType in Enum.GetValues<EquipmentShopType>())
        {
            if (!snapshot.Settings.ContainsKey(shopType))
            {
                throw new InvalidOperationException(
                    $"Equipment shop settings are missing for '{shopType}'."
                );
            }
        }

        if (!snapshot.Products.ContainsKey(EquipmentShopProductKeys.Armor))
        {
            throw new InvalidOperationException("Equipment shop armor product is missing.");
        }

        Interlocked.Exchange(ref _snapshot, snapshot);
    }
}
