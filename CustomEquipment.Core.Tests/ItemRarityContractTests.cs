using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.DatabaseWeapons;
using CustomEquipment.Data.Equipments.Weapons.Equipments;
using CustomEquipment.Data.Equipments.Weapons.Grenades;
using CustomEquipment.Data.GameplayItems;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class ItemRarityContractTests
{
    [Theory]
    [InlineData(ItemRarity.Common)]
    [InlineData(ItemRarity.Legendary)]
    public void DatabaseWeaponAndItsInstancesExposeCatalogRarityThroughTheSharedApi(ItemRarity rarity)
    {
        var definition = new DatabaseWeaponDefinition(
            "weapon_ak47", AccessFlags.All, "Test", "Equipment.Test.Name", "test", "ak47",
            Slot.Primary, WeaponType.Rifle, string.Empty, null, null, null, null, [], rarity);
        var weapon = new DatabaseWeaponItem(definition);

        Assert.Equal(rarity, Assert.IsAssignableFrom<IHasRarity>(weapon).Rarity);
        Assert.Equal(rarity, Assert.IsAssignableFrom<IHasRarity>(weapon.CreateInstance()).Rarity);
        Assert.False((object)weapon is IShopItem);
    }

    [Fact]
    public void GrenadesAndEquipmentExposeRarityFromTheCurrentCatalog()
    {
        var catalog = new GameplayItemCatalog();
        var grenade = new FireNade(catalog);
        var mine = new LaserMine(catalog);

        Assert.Equal(catalog.Get(GameplayItemKeys.FireNade).Rarity,
            Assert.IsAssignableFrom<IHasRarity>(grenade).Rarity);
        Assert.Equal(catalog.Get(GameplayItemKeys.LaserMine).Rarity,
            Assert.IsAssignableFrom<IHasRarity>(mine).Rarity);

        catalog.Replace(GameplayItemDefaults.All.Select(definition =>
            definition with { Rarity = ItemRarity.Legendary }).ToArray());

        Assert.Equal(ItemRarity.Legendary, ((IHasRarity)grenade).Rarity);
        Assert.Equal(ItemRarity.Legendary, ((IHasRarity)mine).Rarity);
    }
}
