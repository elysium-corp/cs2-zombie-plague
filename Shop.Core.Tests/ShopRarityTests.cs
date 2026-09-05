using System.Reflection;
using CustomEquipment.Api;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using Shop.Api.Data;
using Shop.Core.Application;
using Shop.Core.Data;
using Shop.Core.Menus;

namespace Shop.Core.Tests;

public sealed class ShopRarityTests
{
    [Theory]
    [InlineData(ItemRarity.Common, "#B0C3D9")]
    [InlineData(ItemRarity.Uncommon, "#5E98D9")]
    [InlineData(ItemRarity.Rare, "#4B69FF")]
    [InlineData(ItemRarity.Restricted, "#8847FF")]
    [InlineData(ItemRarity.Classified, "#D32CE6")]
    [InlineData(ItemRarity.Elite, "#EB4B4B")]
    [InlineData(ItemRarity.Prototype, "#FF8C00")]
    [InlineData(ItemRarity.Legendary, "#E4AE39")]
    public void CatalogRarityColorsTheNameWithoutAShopPriceContract(ItemRarity rarity, string color)
    {
        var item = new CatalogItem(rarity);
        var provider = Provider(item);
        var actual = provider.GetRarity(Offer("custom_equipment", item.InternalName));

        Assert.Equal(rarity, actual);
        Assert.Equal($"<font color='{color}'>Plasma</font> [<font color='#FFFF00'>300$</font>]",
            ShopMenuText.Offer("Plasma", "300$", actual));
    }

    [Fact]
    public void AllStandardFirearmsUseCommonWithoutConsultingCustomEquipment()
    {
        var provider = new ShopProductProvider(() => throw new InvalidOperationException());

        Assert.Equal(34, StandardWeaponCatalog.Weapons.Count);
        foreach (var key in StandardWeaponCatalog.Weapons.Keys)
        {
            Assert.Equal(ItemRarity.Common, provider.GetRarity(Offer(StandardWeaponCatalog.ProviderKey, key)));
        }
    }

    [Fact]
    public void LegacyShopItemsKeepTheirConfiguredRarity()
    {
        var item = new LegacyShopItem();

        Assert.Equal(ItemRarity.Elite, Provider(item).GetRarity(Offer("custom_equipment", item.InternalName)));
    }

    [Theory]
    [InlineData("cs2_weapon", "weapon_unknown")]
    [InlineData("cs2_weapon", "weapon_hegrenade")]
    [InlineData("builtin", "armor")]
    [InlineData("unknown", "weapon_ak47")]
    public void OtherProductsDoNotInheritStandardWeaponRarity(string providerKey, string itemKey)
    {
        var provider = new ShopProductProvider(() => throw new InvalidOperationException());

        Assert.Null(provider.GetRarity(Offer(providerKey, itemKey)));
    }

    [Fact]
    public void MissingOrUnannotatedCustomItemsStillShowAYellowPrice()
    {
        Assert.Null(Provider(null).GetRarity(Offer("custom_equipment", "missing")));
        Assert.Null(Provider(new PlainItem()).GetRarity(Offer("custom_equipment", "plain")));
        Assert.Equal("Armor [<font color='#FFFF00'>300$</font>]", ShopMenuText.Offer("Armor", "300$", null));
    }

    [Fact]
    public void NameAndPriceCannotInjectMarkupIntoTheirColors()
    {
        Assert.Equal(
            "<font color='#E4AE39'>&lt;Plasma&gt; &amp; AK</font> [<font color='#FFFF00'>&lt;300$&gt;</font>]",
            ShopMenuText.Offer("<Plasma> & AK", "<300$>", ItemRarity.Legendary));
    }

    private static ShopProductProvider Provider(IItem? item)
    {
        var api = DispatchProxy.Create<ICustomEquipmentApi, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)api).Handler = (method, arguments) =>
        {
            Assert.Equal("TryGetRegisteredItem", method.Name);
            arguments![1] = item;
            return item is not null;
        };
        return new ShopProductProvider(() => api);
    }

    private static ShopOfferDefinition Offer(string providerKey, string itemKey) => new(
        new ShopOffer(1, ShopType.Human, providerKey, itemKey, "Equipment.Test.Name",
            null, 300, null, 1, 0, 0, 0, ShopAccessMode.Everyone, new HashSet<string>(), true, 0), null, "{}");

    private class PlainItem : IItem
    {
        public AccessFlags AccessFlags => AccessFlags.All;
        public string DisplayName => "Plasma";
        public string InternalName => "plasma";
        public string SubclassName => string.Empty;
        public Slot Slot => Slot.Primary;
    }

    private sealed class CatalogItem(ItemRarity rarity) : PlainItem, IHasRarity
    {
        public ItemRarity Rarity => rarity;
    }

    private sealed class LegacyShopItem : PlainItem, IShopItem
    {
        public WeaponType WeaponType => WeaponType.Rifle;
        public Price Price { get; } = new() { Item = 300 };
        public ItemRarity Rarity => ItemRarity.Elite;
    }
}
