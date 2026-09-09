using System.Reflection;
using System.Xml.Linq;
using CustomEquipment.Api;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using Shop.Api.Data;
using Shop.Core.Application;
using Shop.Core.Data;
using Shop.Core.Hud;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Tests;

public sealed class ShopHudTests
{
    [Fact]
    public void CatalogUsesOnlyEnabledCurrentSideCategoriesAndOffers()
    {
        var snapshot = Snapshot([
            Offer(1), Offer(2, enabled: false), Offer(3, category: 2),
            Offer(4, type: ShopType.Zombie), Offer(5, category: null)
        ], [Category(1), Category(2, enabled: false)]);
        var view = Project(snapshot, new());
        Assert.Equal(new long[] { 1, 5 }, view.Columns.SelectMany(x => x.Cards).Select(x => x.Offer.Id));
        Assert.Equal("Shop.Hud.Other", view.Columns[1].Title);
    }

    [Fact]
    public void CatalogIncludesStandardShopWeaponsAndRegisteredCustomEquipmentBeforeBuildingPages()
    {
        var items = new Dictionary<string, IItem>
        {
            ["plasma"] = new EquipmentItem("plasma", Slot.Primary),
            ["infection_grenade"] = new EquipmentItem("infection_grenade", Slot.Grenade),
            ["not_in_shop"] = new EquipmentItem("not_in_shop", Slot.Secondary)
        };
        var api = DispatchProxy.Create<ICustomEquipmentApi, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)api).Handler = (method, arguments) =>
        {
            Assert.Equal("TryGetRegisteredItem", method.Name);
            var found = items.TryGetValue((string)arguments![0]!, out var item);
            arguments[1] = item;
            return found;
        };
        var products = new ShopProductProvider(() => api);
        var offers = new[]
        {
            Product(1, "custom_equipment", "plasma", 10),
            Product(2, "custom_equipment", "infection_grenade", 20),
            Product(3, "custom_equipment", "removed_grenade", 20),
            Product(4, "cs2_weapon", "weapon_ak47", 30),
            Product(5, "builtin", "armor", 30),
            Product(6, "custom_equipment", "removed_weapon", 40),
            Product(7, "cs2_weapon", "weapon_hegrenade", 40),
            Product(8, "cs2_weapon", "weapon_unknown", 40)
        };
        var snapshot = Snapshot(offers, [Category(10), Category(20), Category(30), Category(40)]);
        var navigation = new ShopHudNavigation();
        var view = ShopHudCatalog.Project(snapshot, ShopType.Human, navigation, key => key,
            products.IsHudProduct, offer => Card(offer) with { Enabled = false });

        Assert.Equal(new[] { "Category10", "Category20", "Category30" }, view.Columns.Select(x => x.Title));
        Assert.Equal(new long[] { 1, 2, 4, 5 }, view.Columns.SelectMany(x => x.Cards).Select(x => x.Offer.Id));
        Assert.All(view.Columns.SelectMany(x => x.Cards), card => Assert.False(card.Enabled));

        items.Remove("infection_grenade");
        var updated = ShopHudCatalog.Project(snapshot, ShopType.Human, navigation, key => key,
            products.IsHudProduct, Card);
        Assert.Equal(new[] { "Category10", "Category30" }, updated.Columns.Select(x => x.Title));
        Assert.False(ShopHudMenu.SameSlots(view, updated));
    }

    [Fact]
    public void HudDoesNotAddStandardWeaponsWithoutAnEnabledShopOffer()
    {
        var products = new ShopProductProvider(() => throw new InvalidOperationException("Unexpected equipment lookup"));
        var enabled = Product(1, StandardWeaponCatalog.ProviderKey, "weapon_glock", 1);
        var disabled = Product(2, StandardWeaponCatalog.ProviderKey, "weapon_ak47", 1);
        disabled = disabled with { Contract = disabled.Contract with { Enabled = false } };
        var view = ShopHudCatalog.Project(Snapshot([enabled, disabled], [Category(1)]), ShopType.Human,
            new ShopHudNavigation(), key => key, products.IsHudProduct, Card);

        var card = Assert.Single(Assert.Single(view.Columns).Cards);
        Assert.Equal(enabled.Contract, card.Offer);
    }

    [Fact]
    public void EveryStandardFirearmHasAHudIconAndCommonRarityWithoutEquipmentRegistration()
    {
        var products = new ShopProductProvider(() => throw new InvalidOperationException("Unexpected equipment lookup"));
        foreach (var key in StandardWeaponCatalog.Weapons.Keys)
        {
            var offer = Product(1, StandardWeaponCatalog.ProviderKey, key, 1);
            Assert.True(products.IsHudProduct(offer));
            Assert.Equal(ItemRarity.Common, products.GetRarity(offer));
            Assert.NotEqual("equipment", products.GetHudIcon(offer));
            Assert.False(products.IsHudProduct(offer with { Contract = offer.Contract with { ShopType = ShopType.Zombie } }));
        }
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 3)]
    [InlineData(8, 6)]
    public void EveryOfferRemainsReachableBeyondConfiguredColumnsAndRows(int columnCount, int rowCount)
    {
        var categories = Enumerable.Range(1, 10).Select(x => Category(x)).ToArray();
        var offers = categories.SelectMany(x => Enumerable.Range(1, 15).Select(i => Offer(x.Id * 100 + i, x.Id))).ToArray();
        var snapshot = Snapshot(offers, categories);
        snapshot = snapshot with
        {
            Storefronts = snapshot.Storefronts.ToDictionary(x => x.Key, x => x.Value with
            {
                Appearance = ShopHudAppearance.Default with { Columns = columnCount, Rows = rowCount }
            })
        };
        var reached = new HashSet<long>();
        for (var categoryPage = 0; categoryPage < (categories.Length + columnCount - 1) / columnCount; categoryPage++)
        for (var itemPage = 0; itemPage < (15 + rowCount - 1) / rowCount; itemPage++)
        {
            var navigation = new ShopHudNavigation { Page = categoryPage };
            foreach (var category in categories) navigation.ItemPages[category.Id.ToString()] = itemPage;
            var view = Project(snapshot, navigation);
            Assert.InRange(view.Columns.Count, 1, columnCount);
            foreach (var column in view.Columns)
            {
                Assert.InRange(column.Cards.Count, 1, rowCount);
                foreach (var card in column.Cards) Assert.True(reached.Add(card.Offer.Id));
            }
        }
        Assert.Equal(offers.Length, reached.Count);
    }

    [Theory]
    [InlineData(1, 2)]
    [InlineData(2, 3)]
    [InlineData(0, 1)]
    public void CatalogPreservesConfiguredSorting(int mode, long firstId)
    {
        var offers = new[]
        {
            Offer(1) with { Contract = Offer(1).Contract with { DisplayNameKey = "Z", Price = 900, SortOrder = 0 } },
            Offer(2) with { Contract = Offer(2).Contract with { DisplayNameKey = "B", Price = 100, SortOrder = 2 } },
            Offer(3) with { Contract = Offer(3).Contract with { DisplayNameKey = "A", Price = 500, SortOrder = 3 } }
        };
        Assert.Equal(firstId, Project(Snapshot(offers, [Category(1)], (ShopSortMode)mode), new()).Columns[0].Cards[0].Offer.Id);
    }

    [Fact]
    public void RemovedCategoriesClampOldPagesAndDisabledShopHasNoCards()
    {
        var navigation = new ShopHudNavigation { Page = 999 };
        navigation.ItemPages["1"] = 999;
        var view = Project(Snapshot([Offer(1)], [Category(1)]), navigation);
        Assert.Equal(0, view.Page);
        Assert.Equal(0, view.Columns[0].Page);
        Assert.Empty(Project(ShopSnapshot.Empty(), navigation).Columns);
    }

    [Fact]
    public void BalanceChangesKeepSlotsButChangedPricesOrItemsInvalidateOldButtons()
    {
        var view = Project(Snapshot([Offer(1)], [Category(1)]), new());
        var column = view.Columns[0];
        var grey = view with { Columns = [column with { Cards = [column.Cards[0] with { Enabled = false }] }] };
        Assert.True(ShopHudMenu.SameSlots(view, grey));
        var changed = view with { Columns = [column with { Cards = [column.Cards[0] with
            { Offer = column.Cards[0].Offer with { Price = 999 } }] }] };
        Assert.False(ShopHudMenu.SameSlots(view, changed));
        Assert.False(ShopHudMenu.SameSlots(view, Project(Snapshot([Offer(2)], [Category(1)]), new())));
        Assert.False(ShopHudMenu.SameSlots(view, view with { ShopType = ShopType.Zombie }));
    }

    [Theory]
    [InlineData("buymenu", 1)]
    [InlineData("  BUY ak47", 2)]
    [InlineData("\"buy\" ak47", 2)]
    [InlineData("buyrandom", 2)]
    [InlineData("autobuy", 2)]
    [InlineData("rebuy", 2)]
    [InlineData("buy ak47; say x", 2)]
    [InlineData("buy_something", 0)]
    [InlineData("say !weapons", 0)]
    [InlineData("cancelselect", 0)]
    [InlineData("", 0)]
    public void NativePurchaseInterceptorMatchesCommandsExactly(string line, int expected) =>
        Assert.Equal(expected, (int)ShopHudMenu.NativeBuyCommand(line));

    [Theory]
    [InlineData("Buy0", true)]
    [InlineData("Buy47", true)]
    [InlineData("Buy48", false)]
    [InlineData("Buy-1", false)]
    [InlineData("Buy01", false)]
    [InlineData("Buy1;buy ak47", false)]
    [InlineData("Close", false)]
    public void ClickIdsCannotAddressOutsideThePublishedPool(string id, bool expected) =>
        Assert.Equal(expected, ShopHudMenu.TryIndex(id, "Buy", ShopHudCatalog.SlotCount, out _));

    [Fact]
    public void ReusedPlayerSlotNeverInheritsHudInputCapture()
    {
        var session = 10UL;
        var player = DispatchProxy.Create<IPlayer, PlayerStub>();
        ((PlayerStub)(object)player).GetSession = () => session;
        var state = new ShopHudState();
        state.Open(player);
        Assert.True(state.IsOpen(player));
        session++;
        Assert.False(state.IsOpen(player));
        state.Open(player);
        state.Close(player.PlayerID);
        Assert.False(state.IsOpen(player));
    }

    [Theory]
    [InlineData("weapon_ak47", "ak47")]
    [InlineData("weapon_m4a1_silencer", "m4a1_silencer")]
    [InlineData("../bad.svg", "equipment")]
    [InlineData("https://example.com/a.png", "equipment")]
    public void IconsUseOnlyThePackagedAllowlist(string value, string expected) =>
        Assert.Equal(expected, ShopHudIcons.Normalize(value));

    [Fact]
    public void PanoramaContainsOnlyAllowedElementsAndAllServerTargets()
    {
        var xml = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "Fixtures",
            Path.GetFileName(Path.ChangeExtension(ShopHudRuntime.Layout, ".xml"))));
        var rootPanel = Assert.Single(xml.Root!.Elements("Panel"));
        Assert.Null(rootPanel.Attribute("id"));
        Assert.Equal(new[] { "s2r://" + ShopHudRuntime.Style,
            "s2r://" + ShopHudRuntime.IconsStyle },
            xml.Root!.Element("styles")!.Elements("include").Select(x => (string?)x.Attribute("src")));
        // ShopRoot должен оставаться адресуемым потомком для персонального показа HUD.
        Assert.Single(rootPanel.Descendants("Panel").Where(x => (string?)x.Attribute("id") == "ShopRoot"));
        var allowed = new Dictionary<string, string[]>
        {
            ["Panel"] = ["id", "class", "hittest"], ["Label"] = ["id", "class", "hittest", "text"],
            ["Image"] = ["id", "class", "hittest", "src", "texturewidth", "textureheight"],
            ["Button"] = ["id", "class"]
        };
        foreach (var element in xml.Descendants().Where(x => x.Name.LocalName is not ("root" or "styles" or "include")))
        {
            Assert.True(allowed.ContainsKey(element.Name.LocalName));
            foreach (var attribute in element.Attributes()) Assert.Contains(attribute.Name.LocalName, allowed[element.Name.LocalName]);
        }
        var ids = xml.Descendants().Attributes("id").Select(x => x.Value).ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
        Assert.True(ids.Length < 1024);
        for (var slot = 0; slot < ShopHudCatalog.SlotCount; slot++)
        foreach (var prefix in new[] { "Card", "A_Buy", "B_Buy", "Icon", "Name", "Price", "Status" })
            Assert.Contains(prefix + slot, ids);
        for (var column = 0; column < ShopHudCatalog.ColumnCount; column++)
        foreach (var prefix in new[] { "Column", "Category", "Page", "Prev", "Next", "A_Previous", "B_Previous", "A_NextItems", "B_NextItems", "ItemPager" })
            Assert.Contains(prefix + column, ids);
        Assert.Contains("CategoryPager", ids);
        Assert.Contains("Settings", ids);
        Assert.Contains("SettingsPanel", ids);
        for (var index = 0; index < ShopHudPreference.Scales.Length; index++)
        {
            Assert.Contains("SetScale" + index, ids);
            Assert.Contains("ScaleOption" + index, ids);
        }
        foreach (var bank in new[] { "A", "B" })
        foreach (var direction in new[] { "Previous", "Next" })
            Assert.Contains(bank + "_Categories" + direction, ids);
        var css = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures",
            Path.GetFileName(Path.ChangeExtension(ShopHudRuntime.Style, ".css"))));
        Assert.Contains(".BuyHit { width: 100%; height: 100%; visibility: collapse; }", css);
        foreach (var icon in ShopHudIcons.Names) Assert.Contains(".Icon_" + icon + " ", css);
        foreach (var rarity in Enum.GetValues<ItemRarity>()) Assert.Contains(".Rarity" + rarity, css);
    }

    private static ShopHudView Project(ShopSnapshot snapshot, ShopHudNavigation navigation) => ShopHudCatalog.Project(
        snapshot, ShopType.Human, navigation, key => key, _ => true, Card);

    private static ShopHudCard Card(ShopOfferDefinition offer) => new(
        offer.Contract, offer.Contract.DisplayNameKey, offer.Contract.Price.ToString(), "", "ak47", ItemRarity.Common, true);

    private static ShopOfferDefinition Product(long id, string provider, string item, long category) => Offer(id, category) with
    {
        Contract = Offer(id, category).Contract with { ProviderKey = provider, ItemKey = item }
    };

    private static ShopSnapshot Snapshot(IReadOnlyList<ShopOfferDefinition> offers, IReadOnlyList<ShopCategoryDefinition> categories,
        ShopSortMode sortMode = ShopSortMode.Priority) => new(
            new Dictionary<ShopType, ShopStorefrontDefinition> { [ShopType.Human] = new(ShopType.Human, "Human", true, sortMode) },
            categories, offers, "test", DateTimeOffset.UnixEpoch);

    private static ShopCategoryDefinition Category(long id, bool enabled = true) => new(id, ShopType.Human, "c" + id, "Category" + id, null, enabled, (int)id);

    private static ShopOfferDefinition Offer(long id, long? category = 1, bool enabled = true, ShopType type = ShopType.Human) => new(
        new(id, type, "custom_equipment", "weapon_ak47", "Item" + id, category, 100, null, 0, 0, 0, 0,
            ShopAccessMode.Everyone, new HashSet<string>(), enabled, (int)id), null, "{}");

    private sealed class EquipmentItem(string name, Slot slot) : IItem
    {
        public AccessFlags AccessFlags => AccessFlags.All;
        public string DisplayName => name;
        public string InternalName => name;
        public string SubclassName => string.Empty;
        public Slot Slot => slot;
    }

    public class PlayerStub : DispatchProxy
    {
        public Func<ulong> GetSession { get; set; } = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => method?.Name switch
        {
            "get_PlayerID" => 3, "get_SessionId" => GetSession(), _ => throw new InvalidOperationException(method?.Name)
        };
    }
}
