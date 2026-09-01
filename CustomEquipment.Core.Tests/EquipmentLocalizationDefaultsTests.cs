using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Data.Shop;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class EquipmentLocalizationDefaultsTests
{
    [Fact]
    public void ShopDefaults_ExposeMandatoryLocalizationKeys()
    {
        var snapshot = EquipmentShopDefaults.CreateSnapshot();

        Assert.Equal(
            "Equipment.Shop.Human.Title",
            snapshot.Settings[EquipmentShopType.Human].DisplayNameKey
        );
        Assert.Equal(
            "Equipment.Shop.Zombie.Title",
            snapshot.Settings[EquipmentShopType.Zombie].DisplayNameKey
        );

        var categoryKeys = snapshot.Categories
            .Select(category => category.DisplayNameKey)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(key => key, StringComparer.Ordinal)
            .ToArray();
        string[] expectedCategoryKeys =
        [
            "Menu.Equipment.Category.Equipment",
            "Menu.Equipment.Category.Grenade",
            "Menu.Equipment.Category.MachineGun",
            "Menu.Equipment.Category.Pistol",
            "Menu.Equipment.Category.Rifle",
            "Menu.Equipment.Category.Shotgun",
            "Menu.Equipment.Category.SniperRifle",
            "Menu.Equipment.Category.SubmachineGun"
        ];
        Assert.Equal(expectedCategoryKeys, categoryKeys);
        Assert.All(snapshot.Categories, category => Assert.Null(category.DescriptionKey));
        Assert.Equal(
            "Equipment.Item.custom_equipment.armor.Name",
            snapshot.Products[EquipmentShopProductKeys.Armor].DisplayNameKey
        );
    }

    [Theory]
    [InlineData(GameplayItemKeys.BarrierNade, "Equipment.Item.custom_equipment.barrier_nade.Name")]
    [InlineData(GameplayItemKeys.FireNade, "Equipment.Item.custom_equipment.fire_nade.Name")]
    [InlineData(GameplayItemKeys.FrostNade, "Equipment.Item.custom_equipment.frost_nade.Name")]
    [InlineData(GameplayItemKeys.JumpNade, "Equipment.Item.custom_equipment.jump_nade.Name")]
    [InlineData(GameplayItemKeys.ShakeNade, "Equipment.Item.custom_equipment.shake_nade.Name")]
    [InlineData(GameplayItemKeys.LaserMine, "Equipment.Item.custom_equipment.laser_mine.Name")]
    public void GameplayDefaults_ReuseExistingLocalizationKeys(
        string implementationKey,
        string expectedKey
    )
    {
        Assert.Equal(expectedKey, GameplayItemDefaults.Get(implementationKey).DisplayNameKey);
    }
}
