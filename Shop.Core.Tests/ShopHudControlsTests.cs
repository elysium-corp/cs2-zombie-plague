using CustomEquipment.Api.Enums;
using Shop.Api.Data;
using Shop.Core.Hud;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Shop.Core.Tests;

public sealed class ShopHudControlsTests
{
    [Fact]
    public void NativeMenuIsClosedOnceAndCustomOpensOnlyAfterAcknowledgement()
    {
        var native = new ShopHudNativeBuy();
        Assert.Equal(ShopNativeBuyAction.CloseNative, native.Observe(true, false, 0));
        Assert.Equal(ShopNativeBuyAction.None, native.Observe(true, false, 0.5));
        Assert.Equal(ShopNativeBuyAction.None, native.Request(true, 1));
        Assert.Equal(ShopNativeBuyAction.OpenCustom, native.Observe(false, false, 1.5));
        Assert.Equal(ShopNativeBuyAction.None, native.Observe(false, true, 1.6));
        Assert.Equal(ShopNativeBuyAction.CloseNative, native.Observe(true, true, 3));
        Assert.Equal(ShopNativeBuyAction.None, native.Observe(false, false, 3.1));
    }

    [Fact]
    public void DeathOrInfectionDuringCloseAcknowledgementCannotReopenShop()
    {
        var native = new ShopHudNativeBuy();
        native.Observe(true, false, 0);
        native.CancelOpen();
        Assert.Equal(ShopNativeBuyAction.None, native.Observe(false, false, 0.5));
        Assert.False(native.Waiting);
    }

    [Fact]
    public void MissingAcknowledgementDoesNotCaptureMouseOrRepeatedlyToggleNativeMenu()
    {
        var native = new ShopHudNativeBuy();
        native.Observe(true, false, 0);
        Assert.Equal(ShopNativeBuyAction.TimedOut, native.Observe(true, false, 2));
        Assert.Equal(ShopNativeBuyAction.None, native.Observe(true, false, 3));
        Assert.Equal(ShopNativeBuyAction.None, native.Observe(false, false, 4));
        Assert.Equal(ShopNativeBuyAction.CloseNative, native.Observe(true, false, 5));
    }

    [Fact]
    public void PagesAndBalanceUpdatesKeepRuntimeAndRejectQueuedOldPageButtons()
    {
        var offer = new ShopOffer(1, ShopType.Human, "cs2_weapon", "weapon_ak47", "AK", 1,
            100, null, 0, 0, 0, 0, ShopAccessMode.Everyone, new HashSet<string>(), true, 0);
        var card = new ShopHudCard(offer, "AK", "100", "", "ak47", ItemRarity.Common, true);
        var view = new ShopHudView(ShopType.Human, 0, 1, [new("1", "Rifles", 0, 2, [card])]);
        var runtime = new RuntimeStub();
        var created = 0;
        IShopHudRuntime Create() { created++; return runtime; }
        using var pages = new ShopHudPages();
        pages.Bind(view, 0, Create);
        Assert.True(pages.TryButton("A_Buy0", out var buy));
        Assert.Equal("Buy0", buy);
        var grey = view with { Columns = [view.Columns[0] with { Cards = [card with { Enabled = false }] }] };
        pages.Bind(grey, 0, Create);
        Assert.True(pages.TryButton("A_NextItems0", out _));
        var next = view with { Columns = [view.Columns[0] with { Page = 1, Cards = [card with { Offer = offer with { Id = 2 } }] }] };
        pages.Bind(next, 1, Create);
        Assert.False(pages.TryButton("A_Buy0", out _));
        Assert.False(pages.TryButton("A_Confirm0", out _));
        Assert.True(pages.TryButton("B_Confirm0", out _));
        Assert.False(pages.TryButton("A_NextItems0", out _));
        Assert.False(pages.TryButton("A_CategoriesNext", out _));
        Assert.True(pages.TryButton("B_Buy0", out _));
        Assert.Same(runtime, pages.Runtime);
        Assert.Equal(1, created);
        Assert.Equal(0, runtime.Disposals);
        pages.Dispose();
        Assert.Equal(1, runtime.Disposals);
    }

    [Fact]
    public void AutomaticCatalogReplacementRetiresOldEntityInsteadOfReusingOldButtons()
    {
        var view = new ShopHudView(ShopType.Human, 0, 2, [new("1", "Rifles", 0, 1, [])]);
        using var pages = new ShopHudPages();
        var first = new RuntimeStub();
        Assert.True(pages.Bind(view, 0, () => first));
        var changedCatalog = view with { Columns = [new("2", "Pistols", 0, 1, [])] };
        var second = new RuntimeStub();
        Assert.True(pages.Bind(changedCatalog, 0, () => second));
        Assert.Equal(1, first.Disposals);
        Assert.Same(second, pages.Runtime);
        Assert.False(pages.Bind(changedCatalog, 0, () => throw new InvalidOperationException()));
    }

    private sealed class RuntimeStub : IShopHudRuntime
    {
        public int Disposals { get; private set; }
        public bool IsValid => Disposals == 0;
        public bool Owns(CCSCustomHudLayout entity) => false;
        public void Text(string panel, string value) { }
        public void Class(string panel, string name, bool enabled) { }
        public void Capture(bool enabled) { }
        public void Dispose() => Disposals++;
    }
}
