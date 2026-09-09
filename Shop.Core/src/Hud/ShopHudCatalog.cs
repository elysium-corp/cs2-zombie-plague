using CustomEquipment.Api.Enums;
using Economy.Api;
using Localization.Api;
using Shop.Api.Data;
using Shop.Core.Application;
using Shop.Core.Data;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Hud;

internal sealed record ShopHudCard(ShopOffer Offer, string Name, string Price, string Status,
    string Icon, ItemRarity Rarity, bool Enabled, int CooldownSeconds = 0);

internal sealed record ShopHudColumn(string Key, string Title, int Page, int PageCount,
    IReadOnlyList<ShopHudCard> Cards);

internal sealed record ShopHudView(ShopType ShopType, int Page, int PageCount,
    IReadOnlyList<ShopHudColumn> Columns);

internal sealed class ShopHudNavigation
{
    public int Page { get; set; }
    public Dictionary<string, int> ItemPages { get; } = new(StringComparer.Ordinal);
}

internal sealed class ShopHudCatalog(
    ShopSnapshotCache cache,
    ShopAccessEvaluator access,
    ShopProductProvider products,
    Func<ILocalizationApi> localization,
    Func<IEconomyApi> economy)
{
    internal const int ColumnCount = 8;
    internal const int RowCount = 6;
    internal const int SlotCount = ColumnCount * RowCount;

    public bool CanOpen(IPlayer player) => player.IsValid && !player.IsFakeClient && player.IsAlive
        && player.Controller.Team is Team.T or Team.CT
        && cache.Current.Storefronts.TryGetValue(access.GetShopType(player), out var store) && store.Enabled;

    public ShopHudView Build(IPlayer player, ShopHudNavigation navigation) => Project(
        cache.Current, access.GetShopType(player), navigation, key => Text(player, key),
        products.IsHudProduct, offer => Card(player, offer));

    public string Text(IPlayer player, string key) => localization().GetForPlayer(player, key) ?? key;

    public string Title(IPlayer player) => Text(player, cache.Current.Storefronts[access.GetShopType(player)].TitleKey);

    public string Balance(IPlayer player) => localization().FormatForPlayer(player, "Shop.Menu.Price",
        new Dictionary<string, object?> { ["price"] = economy().GetBalance(player) }) ?? string.Empty;

    private ShopHudCard Card(IPlayer player, ShopOfferDefinition offer)
    {
        var availability = access.Evaluate(player, offer);
        var status = string.Empty;
        if (!availability.Allowed && availability.Reason is not
            (ShopAvailabilityReason.InsufficientFunds or ShopAvailabilityReason.CooldownActive))
            status = Text(player, ShopLocalization.AvailabilityKey(availability.Reason));
        return new ShopHudCard(offer.Contract, Text(player, offer.Contract.DisplayNameKey),
            localization().FormatForPlayer(player, "Shop.Menu.Price",
                new Dictionary<string, object?> { ["price"] = offer.Contract.Price }) ?? offer.Contract.Price.ToString(),
            status, products.GetHudIcon(offer), products.GetRarity(offer) ?? ItemRarity.Common, availability.Allowed,
            (int)Math.Ceiling(access.RemainingCooldown(player, offer.Contract).TotalSeconds));
    }

    internal static ShopHudView Project(ShopSnapshot snapshot, ShopType type, ShopHudNavigation navigation,
        Func<string, string> text, Func<ShopOfferDefinition, bool> isHudProduct,
        Func<ShopOfferDefinition, ShopHudCard> card)
    {
        if (!snapshot.Storefronts.TryGetValue(type, out var store) || !store.Enabled)
            return new(type, 0, 1, []);

        var columnCount = store.Appearance.Columns;
        var rowCount = store.Appearance.Rows;

        // Показываем зарегистрированные предметы, обычные пушки и встроенную броню
        // из предложений Shop. Доступность покупки определяет только состояние карточки.
        var offers = snapshot.Offers.Where(x => x.ShopType == type && x.Enabled && isHudProduct(x)).ToArray();
        var categories = snapshot.Categories.Where(x => x.ShopType == type && x.Enabled)
            .OrderBy(x => x.SortOrder).ThenBy(x => text(x.DisplayNameKey), StringComparer.CurrentCultureIgnoreCase)
            .Where(x => offers.Any(offer => offer.CategoryId == x.Id))
            .Select(x => (Key: x.Id.ToString(), Title: text(x.DisplayNameKey), Id: (long?)x.Id)).ToList();
        if (offers.Any(x => x.CategoryId is null))
            categories.Add(("uncategorized", text("Shop.Hud.Other"), null));

        var pageCount = Math.Max(1, (categories.Count + columnCount - 1) / columnCount);
        navigation.Page = Math.Clamp(navigation.Page, 0, pageCount - 1);
        var columns = new List<ShopHudColumn>();
        foreach (var category in categories.Skip(navigation.Page * columnCount).Take(columnCount))
        {
            var source = offers.Where(x => x.CategoryId == category.Id);
            // Сортировка совпадает с настройкой существующей витрины магазина.
            var sorted = store.SortMode switch
            {
                ShopSortMode.Price => source.OrderBy(x => x.Contract.Price)
                    .ThenBy(x => text(x.Contract.DisplayNameKey), StringComparer.CurrentCultureIgnoreCase),
                ShopSortMode.Alphabetical => source.OrderBy(x => text(x.Contract.DisplayNameKey), StringComparer.CurrentCultureIgnoreCase)
                    .ThenBy(x => x.Contract.Price),
                _ => source.OrderBy(x => x.Contract.SortOrder)
                    .ThenBy(x => text(x.Contract.DisplayNameKey), StringComparer.CurrentCultureIgnoreCase)
            };
            var items = sorted.ThenBy(x => x.Id).ToArray();
            var count = Math.Max(1, (items.Length + rowCount - 1) / rowCount);
            var page = Math.Clamp(navigation.ItemPages.GetValueOrDefault(category.Key), 0, count - 1);
            navigation.ItemPages[category.Key] = page;
            columns.Add(new(category.Key, category.Title, page, count,
                items.Skip(page * rowCount).Take(rowCount).Select(card).ToArray()));
        }
        return new(type, navigation.Page, pageCount, columns);
    }
}
