using System.Collections.Frozen;
using System.Text.Json;
using Shop.Api.Data;
using Shop.Core.Configuration;
using Shop.Core.Database.Entities;

namespace Shop.Core.Data;

internal static class ShopSnapshotMapper
{
    public static ShopSnapshot FromDatabase(
        IEnumerable<ShopStorefrontEntity> storefrontEntities,
        IEnumerable<ShopCategoryEntity> categoryEntities,
        IEnumerable<ShopOfferEntity> offerEntities)
    {
        var storefronts = storefrontEntities.Select(entity => new ShopStorefrontDefinition(
            ParseShopType(entity.ShopType),
            LocalizationKey(entity.TitleKey, nameof(entity.TitleKey)),
            entity.Enabled,
            ParseSortMode(entity.SortMode))).ToArray();
        var categories = categoryEntities.Select(entity => new ShopCategoryDefinition(
            entity.Id,
            ParseShopType(entity.ShopType),
            Identifier(entity.Key, nameof(entity.Key)),
            LocalizationKey(entity.DisplayNameKey, nameof(entity.DisplayNameKey)),
            OptionalLocalizationKey(entity.DescriptionKey, nameof(entity.DescriptionKey)),
            entity.Enabled,
            entity.SortOrder)).ToArray();
        var offers = offerEntities.Select(entity => MapOffer(
            entity.Id,
            entity.ShopType,
            entity.ProviderKey,
            entity.ItemKey,
            entity.DisplayNameKey,
            entity.CategoryId,
            entity.DescriptionKey,
            entity.Price,
            entity.AmmoPrice,
            entity.AmmoAmount,
            entity.MaxPurchasesPerRound,
            entity.MaxPurchasesPerMap,
            entity.CooldownSeconds,
            entity.AccessMode,
            entity.Privileges.Select(privilege => privilege.PrivilegeKey),
            entity.Enabled,
            entity.SortOrder,
            entity.SettingsJson)).ToArray();

        return Build(storefronts, categories, offers, "postgresql");
    }

    public static ShopSnapshot FromFallback(ShopFallbackConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        if (config.SchemaVersion != 1 || config.GeneratedAt == DateTimeOffset.UnixEpoch)
        {
            throw new InvalidDataException("Fallback-конфигурация магазина имеет неподдерживаемую схему или дату.");
        }

        var storefronts = config.Storefronts.Select(item => new ShopStorefrontDefinition(
            ParseShopType(item.ShopType),
            LocalizationKey(item.TitleKey, nameof(item.TitleKey)),
            item.Enabled,
            ParseSortMode(item.SortMode))).ToArray();
        var categories = config.Categories.Select(item => new ShopCategoryDefinition(
            item.Id,
            ParseShopType(item.ShopType),
            Identifier(item.Key, nameof(item.Key)),
            LocalizationKey(item.DisplayNameKey, nameof(item.DisplayNameKey)),
            OptionalLocalizationKey(item.DescriptionKey, nameof(item.DescriptionKey)),
            item.Enabled,
            item.SortOrder)).ToArray();
        var offers = config.Offers.Select(item => MapOffer(
            item.Id,
            item.ShopType,
            item.ProviderKey,
            item.ItemKey,
            item.DisplayNameKey,
            item.CategoryId,
            item.DescriptionKey,
            item.Price,
            item.AmmoPrice,
            item.AmmoAmount,
            item.MaxPurchasesPerRound,
            item.MaxPurchasesPerMap,
            item.CooldownSeconds,
            item.AccessMode,
            item.RequiredPrivileges,
            item.Enabled,
            item.SortOrder,
            item.SettingsJson)).ToArray();

        return Build(storefronts, categories, offers, "shop.json");
    }

    private static ShopSnapshot Build(
        IReadOnlyCollection<ShopStorefrontDefinition> storefronts,
        IReadOnlyList<ShopCategoryDefinition> categories,
        IReadOnlyList<ShopOfferDefinition> offers,
        string source)
    {
        if (storefronts.Select(item => item.ShopType).Distinct().Count() != storefronts.Count)
        {
            throw new InvalidDataException("Стороны storefront должны быть уникальными.");
        }

        var storefrontMap = storefronts.ToDictionary(item => item.ShopType);

        foreach (var shopType in Enum.GetValues<ShopType>())
        {
            if (!storefrontMap.ContainsKey(shopType))
            {
                throw new InvalidDataException($"Отсутствует storefront '{shopType}'.");
            }
        }

        if (categories.Any(item => item.Id <= 0) ||
            categories.Select(item => item.Id).Distinct().Count() != categories.Count ||
            offers.Select(item => item.Id).Distinct().Count() != offers.Count)
        {
            throw new InvalidDataException(
                "Идентификаторы категорий и офферов должны быть положительными и уникальными.");
        }

        if (categories
                .GroupBy(item => (item.ShopType, item.Key))
                .Any(group => group.Count() > 1) ||
            offers
                .GroupBy(item => (
                    item.ShopType,
                    item.Contract.ProviderKey,
                    item.Contract.ItemKey),
                    ShopProductKeyComparer.Instance)
                .Any(group => group.Count() > 1))
        {
            throw new InvalidDataException(
                "Категории и товары должны быть уникальными внутри своей витрины.");
        }

        var categoryMap = categories.ToDictionary(item => item.Id);
        foreach (var offer in offers)
        {
            if (offer.CategoryId is { } categoryId &&
                (!categoryMap.TryGetValue(categoryId, out var category) ||
                 category.ShopType != offer.ShopType))
            {
                throw new InvalidDataException(
                    $"Оффер '{offer.Id}' ссылается на отсутствующую категорию своей стороны.");
            }
        }

        return new ShopSnapshot(storefrontMap, categories, offers, source, DateTimeOffset.UtcNow);
    }

    private static ShopOfferDefinition MapOffer(
        long id,
        string shopType,
        string providerKey,
        string itemKey,
        string displayNameKey,
        long? categoryId,
        string? descriptionKey,
        int price,
        int? ammoPrice,
        int ammoAmount,
        int maxPurchasesPerRound,
        int maxPurchasesPerMap,
        int cooldownSeconds,
        string accessMode,
        IEnumerable<string> requiredPrivileges,
        bool enabled,
        int sortOrder,
        string settingsJson)
    {
        if (id <= 0 || price < 0 || ammoPrice < 0 || ammoAmount <= 0 ||
            maxPurchasesPerRound < 0 || maxPurchasesPerMap < 0 || cooldownSeconds < 0)
        {
            throw new InvalidDataException($"Оффер '{id}' содержит недопустимые числовые значения.");
        }

        var normalizedProviderKey = Identifier(providerKey, nameof(providerKey));
        var normalizedItemKey = Required(itemKey, nameof(itemKey), 128);
        using var document = JsonDocument.Parse(string.IsNullOrWhiteSpace(settingsJson) ? "{}" : settingsJson);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"Settings оффера '{id}' должны быть JSON-объектом.");
        }
        ValidateKnownProductSettings(id, normalizedProviderKey, normalizedItemKey, document.RootElement);

        var mode = ParseAccessMode(accessMode);
        var privileges = requiredPrivileges
            .Select(value => PrivilegeKey(value, nameof(requiredPrivileges)))
            .ToFrozenSet(StringComparer.OrdinalIgnoreCase);

        if (mode == ShopAccessMode.Everyone && privileges.Count > 0 ||
            mode != ShopAccessMode.Everyone && privileges.Count == 0)
        {
            throw new InvalidDataException($"Оффер '{id}' имеет несогласованный режим доступа.");
        }

        return new ShopOfferDefinition(
            new ShopOffer(
                id,
                ParseShopType(shopType),
                normalizedProviderKey,
                normalizedItemKey,
                LocalizationKey(displayNameKey, nameof(displayNameKey)),
                categoryId,
                price,
                ammoPrice,
                ammoAmount,
                maxPurchasesPerRound,
                maxPurchasesPerMap,
                cooldownSeconds,
                mode,
                privileges,
                enabled,
                sortOrder),
            OptionalLocalizationKey(descriptionKey, nameof(descriptionKey)),
            document.RootElement.GetRawText());
    }

    private static void ValidateKnownProductSettings(
        long offerId,
        string providerKey,
        string itemKey,
        JsonElement settings)
    {
        if (providerKey != "builtin" || itemKey != "armor")
        {
            return;
        }

        var hasAmount = settings.TryGetProperty("armor_amount", out var amountElement) ||
                        settings.TryGetProperty("armorAmount", out amountElement);
        if (hasAmount &&
            (amountElement.ValueKind != JsonValueKind.Number ||
             !amountElement.TryGetInt32(out var amount) ||
             amount is < 1 or > 100))
        {
            throw new InvalidDataException(
                $"Оффер брони '{offerId}' должен содержать armor_amount от 1 до 100.");
        }
    }

    internal static ShopType ParseShopType(string value) => value.Trim().ToLowerInvariant() switch
    {
        "human" => ShopType.Human,
        "zombie" => ShopType.Zombie,
        _ => throw new InvalidDataException($"Неизвестная сторона магазина '{value}'.")
    };

    internal static ShopSortMode ParseSortMode(string value) => value.Trim().ToLowerInvariant() switch
    {
        "priority" => ShopSortMode.Priority,
        "price" => ShopSortMode.Price,
        "alphabetical" => ShopSortMode.Alphabetical,
        _ => throw new InvalidDataException($"Неизвестный порядок магазина '{value}'.")
    };

    internal static ShopAccessMode ParseAccessMode(string value) => value.Trim().ToLowerInvariant() switch
    {
        "everyone" => ShopAccessMode.Everyone,
        "any" => ShopAccessMode.Any,
        "all" => ShopAccessMode.All,
        _ => throw new InvalidDataException($"Неизвестный режим доступа '{value}'.")
    };

    private static string Identifier(string value, string name)
    {
        var result = Required(value, name, 64).ToLowerInvariant();
        if (!char.IsAsciiLetterOrDigit(result[0]) ||
            !result.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-'))
        {
            throw new InvalidDataException($"'{name}' содержит недопустимый идентификатор.");
        }

        return result;
    }

    private static string PrivilegeKey(string value, string name)
    {
        var result = Required(value, name, 129).ToLowerInvariant();
        if (!result.Contains('.') ||
            !result.All(character => char.IsAsciiLetterOrDigit(character) || character is '_' or '-' or '.'))
        {
            throw new InvalidDataException($"'{name}' содержит недопустимый ключ привилегии.");
        }

        return result;
    }

    private static string LocalizationKey(string value, string name)
    {
        var result = Required(value, name, 191);
        var segments = result.Split('.');
        if (segments.Any(segment =>
                segment.Length == 0 ||
                segment[0] is not (>= 'A' and <= 'Z') && !char.IsAsciiDigit(segment[0]) ||
                !segment.All(char.IsAsciiLetterOrDigit)))
        {
            throw new InvalidDataException(
                $"'{name}' должен содержать канонический ключ локализации с сегментами в PascalCase.");
        }

        return result;
    }

    private static string? OptionalLocalizationKey(string? value, string name) =>
        string.IsNullOrWhiteSpace(value) ? null : LocalizationKey(value, name);

    private static string Required(string value, string name, int maxLength)
    {
        var result = value?.Trim() ?? string.Empty;
        if (result.Length == 0 || result.Length > maxLength)
        {
            throw new InvalidDataException($"'{name}' не заполнено или превышает {maxLength} символов.");
        }

        return result;
    }

    private sealed class ShopProductKeyComparer : IEqualityComparer<(ShopType, string, string)>
    {
        public static readonly ShopProductKeyComparer Instance = new();

        public bool Equals(
            (ShopType, string, string) left,
            (ShopType, string, string) right) =>
            left.Item1 == right.Item1 &&
            left.Item2.Equals(right.Item2, StringComparison.OrdinalIgnoreCase) &&
            left.Item3.Equals(right.Item3, StringComparison.OrdinalIgnoreCase);

        public int GetHashCode((ShopType, string, string) value) => HashCode.Combine(
            value.Item1,
            StringComparer.OrdinalIgnoreCase.GetHashCode(value.Item2),
            StringComparer.OrdinalIgnoreCase.GetHashCode(value.Item3));
    }
}
