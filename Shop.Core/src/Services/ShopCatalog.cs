using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using Microsoft.Extensions.Options;
using Shop.Api.Data;
using Shop.Core.Data.Configs;

namespace Shop.Core.Services;

internal sealed class ShopCatalog(
    ICustomEquipmentApi equipmentApi,
    IOptionsMonitor<ShopConfig> configMonitor
) : IShopCatalog
{
    private static readonly IReadOnlyCollection<EquipmentCategory> Categories =
        Array.AsReadOnly(Enum.GetValues<EquipmentCategory>());

    public IReadOnlyCollection<EquipmentCategory> GetCategories() => Categories;

    public IReadOnlyCollection<ShopItem> GetItems()
    {
        var config = configMonitor.CurrentValue;

        if (!config.Enabled)
        {
            return [];
        }

        var overrides = new Dictionary<string, ShopItemOverride>(StringComparer.OrdinalIgnoreCase);

        foreach (var (itemId, itemOverride) in config.Items)
        {
            overrides[itemId] = itemOverride;
        }

        return equipmentApi
            .GetItems()
            .Select(item => CreateShopItem(item, config, overrides))
            .Where(item => item is not null)
            .Select(item => item!)
            .OrderBy(item => item.Category)
            .ThenBy(item => item.DisplayName, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public IReadOnlyCollection<ShopItem> GetItems(EquipmentCategory category)
    {
        return GetItems()
            .Where(item => item.Category == category)
            .ToArray();
    }

    public bool TryGetItem(string itemId, out ShopItem? item)
    {
        item = null;

        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        item = GetItems().FirstOrDefault(candidate =>
            string.Equals(candidate.Id, itemId, StringComparison.OrdinalIgnoreCase)
        );

        return item is not null;
    }

    private static ShopItem? CreateShopItem(
        EquipmentItem equipmentItem,
        ShopConfig config,
        IReadOnlyDictionary<string, ShopItemOverride> overrides
    )
    {
        overrides.TryGetValue(equipmentItem.Id, out var itemOverride);

        if (itemOverride is { Enabled: false })
        {
            return null;
        }

        var price = itemOverride?.Price ?? config.GetDefaultPrice(equipmentItem.Category);
        if (price < 0)
        {
            throw new InvalidOperationException($"Shop price for '{equipmentItem.Id}' cannot be negative.");
        }

        var displayName = string.IsNullOrWhiteSpace(itemOverride?.DisplayName)
            ? equipmentItem.DisplayName
            : itemOverride.DisplayName;

        return new ShopItem(
            Id: equipmentItem.Id,
            EquipmentItemId: equipmentItem.Id,
            DisplayName: displayName!,
            Category: equipmentItem.Category,
            Price: price
        );
    }
}
