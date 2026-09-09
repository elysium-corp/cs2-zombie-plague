using System.Reflection;
using CustomEquipment.Api;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Utils;
using CustomEquipment.Data.Equipments.Models;
using Shop.Api.Data;
using Shop.Core.Application;
using Shop.Core.Data;

namespace Shop.Core.Tests;

public sealed class EquipmentHudIconTests
{
    private static readonly string CustomPath = "panorama/images/custom_game/equipment/" + new string('a', 64) + ".vsvg";

    [Fact]
    public void UploadedIconAndCmsExportUseTheSameCssKey()
    {
        var item = new Weapon(CustomPath);
        Assert.Equal(CustomPath, EquipmentHudIcon.Resolve(item, _ => true));
        Assert.Equal("custom_072a4256c79fce71f0bc3027630be2a1", Provider(item, _ => true).GetHudIcon(Offer("custom_equipment", "plasma")));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("../icon.vsvg")]
    [InlineData("https://site/icon.vsvg")]
    [InlineData("panorama/images/../icon.vsvg")]
    [InlineData("panorama/images/icon.vsvg\");bad")]
    public void MissingOrInvalidIconUsesBaseWeapon(string? path)
    {
        var item = new Weapon(path);
        Assert.Equal("panorama/images/icons/equipment/ak47.vsvg", EquipmentHudIcon.Resolve(item));
        Assert.Equal("ak47", Provider(item, _ => true).GetHudIcon(Offer("custom_equipment", "plasma")));
    }

    [Fact]
    public void UndeployedCustomVsvgUsesBaseWeaponInsteadOfAnEmptyCard()
    {
        var item = new Weapon(CustomPath);
        Assert.Equal("panorama/images/icons/equipment/ak47.vsvg", EquipmentHudIcon.Resolve(item, _ => false));
        Assert.Equal("ak47", Provider(item, _ => false).GetHudIcon(Offer("custom_equipment", "plasma")));
    }

    [Fact]
    public void BuiltinArmorHasAHudCardWithoutCustomEquipmentRegistration()
    {
        var provider = new ShopProductProvider(() => throw new InvalidOperationException("Armor is built in"));
        var offer = Offer("builtin", "armor");
        Assert.True(provider.IsHudProduct(offer));
        Assert.Equal("kevlar", provider.GetHudIcon(offer));
        Assert.False(provider.IsHudProduct(Offer("builtin", "unknown")));
    }

    [Fact]
    public void OfferIconOverridesEquipmentAndMissingResourcesFallBackToWeapon()
    {
        var provider = Provider(new Weapon(CustomPath), _ => true);
        var offer = Offer("custom_equipment", "plasma");
        Assert.Equal("awp", provider.GetHudIcon(offer with { SettingsJson = """{"hud_icon":"awp"}""" }));
        foreach (var invalid in new[] { "{}", "[]", "null", "{", """{"hud_icon":"auto"}""", """{"hud_icon":"https://site/x"}""" })
            Assert.Equal("ak47", Provider(new Weapon(null), _ => false).GetHudIcon(offer with { SettingsJson = invalid }));
        var custom = offer with { SettingsJson = System.Text.Json.JsonSerializer.Serialize(new { hud_icon = CustomPath }) };
        Assert.Equal(EquipmentHudIcon.CssName(CustomPath), provider.GetHudIcon(custom));
        Assert.Equal("ak47", Provider(new Weapon(null), _ => false).GetHudIcon(custom));
    }

    private static ShopProductProvider Provider(IItem item, Func<string, bool> exists)
    {
        var api = DispatchProxy.Create<ICustomEquipmentApi, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)api).Handler = (_, arguments) => { arguments![1] = item; return true; };
        return new ShopProductProvider(() => api, exists);
    }

    private static ShopOfferDefinition Offer(string provider, string key) => new(new ShopOffer(1, ShopType.Human,
        provider, key, "Shop.Item.Name", null, 300, null, 1, 0, 0, 0, ShopAccessMode.Everyone,
        new HashSet<string>(), true, 0), null, "{\"armor_amount\":50}");

    private sealed class Weapon(string? icon) : IWeapon, IHasHudIcon
    {
        public string? HudIconPath => icon;
        public string InheritorName => "weapon_ak47";
        public string InternalName => "plasma";
        public string DisplayName => "Plasma";
        public string SubclassName => "plasma";
        public string Model => "";
        public AccessFlags AccessFlags => AccessFlags.All;
        public Slot Slot => Slot.Primary;
        public WeaponType WeaponType => WeaponType.Rifle;
        public WeaponDamage? WeaponDamage => null;
        public WeaponTiming? WeaponTiming => null;
        public Ammunition? Ammunition => null;
    }
}
