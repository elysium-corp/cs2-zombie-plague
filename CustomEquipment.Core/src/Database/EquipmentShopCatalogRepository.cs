using System.Text.Json;
using System.Text.Json.Serialization;
using CustomEquipment.Data.Shop;
using CustomEquipment.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomEquipment.Database;

internal sealed class EquipmentShopCatalogRepository(
    IDbContextFactory<CustomEquipmentDbContext> contextFactory
) : IEquipmentShopCatalogRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = false,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow
    };

    public EquipmentShopSnapshot GetSnapshot()
    {
        using var context = contextFactory.CreateDbContext();

        var settings = context.ShopSettings
            .AsNoTracking()
            .ToArray()
            .Select(MapSettings)
            .ToDictionary(definition => definition.ShopType);
        var categories = context.ShopCategories
            .AsNoTracking()
            .OrderBy(category => category.SortOrder)
            .ThenBy(category => category.Id)
            .ToArray()
            .Select(MapCategory)
            .ToArray();
        var categoryIds = categories.Select(category => category.Id).ToHashSet();
        var listings = context.ShopListings
            .AsNoTracking()
            .OrderBy(listing => listing.SortOrder)
            .ThenBy(listing => listing.Id)
            .ToArray()
            .Select(MapListing)
            .ToArray();
        var roleLimits = context.ShopRoleLimits
            .AsNoTracking()
            .OrderBy(limit => limit.SortOrder)
            .ThenBy(limit => limit.Id)
            .ToArray()
            .Select(MapRoleLimit)
            .ToArray();
        var products = context.ShopProducts
            .AsNoTracking()
            .OrderBy(product => product.SortOrder)
            .ToArray()
            .Select(MapProduct)
            .ToDictionary(product => product.ImplementationKey, StringComparer.Ordinal);

        var missingCategory = listings.FirstOrDefault(listing => !categoryIds.Contains(listing.CategoryId));

        if (missingCategory is not null)
        {
            throw new InvalidOperationException(
                $"Equipment shop listing '{missingCategory.Id}' references an unknown category."
            );
        }

        foreach (var listing in listings)
        {
            var category = categories.Single(candidate => candidate.Id == listing.CategoryId);

            if (category.ShopType != listing.ShopType)
            {
                throw new InvalidOperationException(
                    $"Equipment shop listing '{listing.Id}' and its category belong to different shops."
                );
            }
        }

        foreach (var shopType in Enum.GetValues<EquipmentShopType>())
        {
            if (!settings.ContainsKey(shopType))
            {
                throw new InvalidOperationException(
                    $"Equipment shop settings are missing for '{shopType}'."
                );
            }
        }

        if (!products.TryGetValue(EquipmentShopProductKeys.Armor, out var armor) ||
            !armor.InternalName.Equals(EquipmentShopItemKeys.Armor, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Equipment shop armor product is missing or has an invalid ID.");
        }

        return new EquipmentShopSnapshot(
            settings,
            categories,
            listings,
            roleLimits,
            products
        );
    }

    private static EquipmentShopSettingsDefinition MapSettings(EquipmentShopSettingsEntity entity)
    {
        var shopType = ParseShopType(entity.ShopType);
        var displayName = Required(entity.DisplayName, nameof(entity.DisplayName), 128);
        RequireLimit(entity.MaxPurchasesPerRound, nameof(entity.MaxPurchasesPerRound));
        RequireLimit(entity.MaxPurchasesPerMap, nameof(entity.MaxPurchasesPerMap));

        return new EquipmentShopSettingsDefinition(
            shopType,
            displayName,
            entity.Enabled,
            entity.MaxPurchasesPerRound,
            entity.MaxPurchasesPerMap
        );
    }

    private static EquipmentShopCategoryDefinition MapCategory(EquipmentShopCategoryEntity entity)
    {
        var key = Required(entity.Key, nameof(entity.Key), 64).ToLowerInvariant();

        if (!key.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '_' or '-'
            ))
        {
            throw new InvalidOperationException($"Equipment shop category '{entity.Id}' has an invalid key.");
        }

        return new EquipmentShopCategoryDefinition(
            entity.Id,
            ParseShopType(entity.ShopType),
            key,
            Required(entity.DisplayName, nameof(entity.DisplayName), 128),
            Optional(entity.Description, 512),
            entity.Enabled,
            entity.SortOrder
        );
    }

    private static EquipmentShopListingDefinition MapListing(EquipmentShopListingEntity entity)
    {
        var internalName = Required(entity.ItemInternalName, nameof(entity.ItemInternalName), 128);
        RequireLimit(entity.Price, nameof(entity.Price));
        RequireLimit(entity.MaxPurchasesPerRound, nameof(entity.MaxPurchasesPerRound));
        RequireLimit(entity.MaxPurchasesPerMap, nameof(entity.MaxPurchasesPerMap));

        return new EquipmentShopListingDefinition(
            entity.Id,
            ParseShopType(entity.ShopType),
            internalName,
            entity.CategoryId,
            Optional(entity.Description, 1_024),
            entity.Price,
            entity.MaxPurchasesPerRound,
            entity.MaxPurchasesPerMap,
            entity.Enabled,
            entity.SortOrder,
            ParseListingSettings(internalName, entity.SettingsJson)
        );
    }

    private static EquipmentShopRoleLimitDefinition MapRoleLimit(EquipmentShopRoleLimitEntity entity)
    {
        var privilegeKey = Required(entity.PrivilegeKey, nameof(entity.PrivilegeKey), 129)
            .ToLowerInvariant();

        if (!privilegeKey.Contains('.') ||
            !privilegeKey.All(character =>
                char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.'
            ))
        {
            throw new InvalidOperationException(
                $"Equipment shop role limit '{entity.Id}' has an invalid privilege key."
            );
        }

        RequireLimit(entity.MaxPurchasesPerRound, nameof(entity.MaxPurchasesPerRound));
        RequireLimit(entity.MaxPurchasesPerMap, nameof(entity.MaxPurchasesPerMap));

        return new EquipmentShopRoleLimitDefinition(
            entity.Id,
            ParseShopType(entity.ShopType),
            privilegeKey,
            entity.MaxPurchasesPerRound,
            entity.MaxPurchasesPerMap,
            entity.Enabled,
            entity.SortOrder
        );
    }

    private static EquipmentShopProductDefinition MapProduct(EquipmentShopProductEntity entity)
    {
        return new EquipmentShopProductDefinition(
            Required(entity.ImplementationKey, nameof(entity.ImplementationKey), 64),
            Required(entity.InternalName, nameof(entity.InternalName), 128),
            Required(entity.DisplayName, nameof(entity.DisplayName), 128),
            entity.Enabled,
            entity.SortOrder
        );
    }

    private static IEquipmentShopListingSettings ParseListingSettings(
        string internalName,
        string json
    )
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new InvalidOperationException(
                $"Equipment shop listing '{internalName}' has empty settings."
            );
        }

        try
        {
            if (internalName.Equals(EquipmentShopItemKeys.Armor, StringComparison.OrdinalIgnoreCase))
            {
                var settings = JsonSerializer.Deserialize<ArmorEquipmentShopListingSettings>(
                    json,
                    JsonOptions
                ) ?? throw new JsonException("Armor settings are empty.");

                if (settings.ArmorAmount is < 1 or > 100)
                {
                    throw new InvalidOperationException(
                        "Equipment shop armor amount must be between 1 and 100."
                    );
                }

                return settings;
            }

            using var document = JsonDocument.Parse(json);

            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new JsonException("Listing settings must be a JSON object.");
            }

            return EmptyEquipmentShopListingSettings.Instance;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                $"Equipment shop listing '{internalName}' has invalid settings JSON.",
                exception
            );
        }
    }

    private static EquipmentShopType ParseShopType(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "human" => EquipmentShopType.Human,
            "zombie" => EquipmentShopType.Zombie,
            _ => throw new InvalidOperationException($"Unknown equipment shop type '{value}'.")
        };
    }

    private static void RequireLimit(int value, string field)
    {
        if (value < 0)
        {
            throw new InvalidOperationException($"{field} cannot be negative.");
        }
    }

    private static string Required(string? value, string field, int maximumLength)
    {
        var trimmed = value?.Trim();

        return string.IsNullOrWhiteSpace(trimmed) || trimmed.Length > maximumLength
            ? throw new InvalidOperationException($"{field} is empty or too long.")
            : trimmed;
    }

    private static string Optional(string? value, int maximumLength)
    {
        var trimmed = value?.Trim() ?? string.Empty;

        return trimmed.Length > maximumLength
            ? throw new InvalidOperationException("Equipment shop text is too long.")
            : trimmed;
    }
}
