using CustomEquipment.Data.Shop;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class EquipmentShopRuntimeCatalogTests
{
    [Fact]
    public void Defaults_KeepHumanAndZombieStorefrontsIndependent()
    {
        var catalog = new EquipmentShopRuntimeCatalog();
        var humanListings = catalog.GetListings(EquipmentShopType.Human);
        var zombieListings = catalog.GetListings(EquipmentShopType.Zombie);

        Assert.Contains(humanListings, listing =>
            listing.ItemInternalName == "custom_equipment:fire_nade"
        );
        Assert.DoesNotContain(zombieListings, listing =>
            listing.ItemInternalName == "custom_equipment:fire_nade"
        );
        Assert.Contains(zombieListings, listing =>
            listing.ItemInternalName == "custom_equipment:jump_nade"
        );
        Assert.DoesNotContain(humanListings, listing =>
            listing.ItemInternalName == "custom_equipment:jump_nade"
        );
        Assert.Contains(humanListings, listing =>
            listing.ItemInternalName == EquipmentShopItemKeys.Armor
        );
        Assert.DoesNotContain(zombieListings, listing =>
            listing.ItemInternalName == EquipmentShopItemKeys.Armor
        );
    }

    [Fact]
    public void Replace_PublishesSettingsForBothStorefronts()
    {
        var catalog = new EquipmentShopRuntimeCatalog();
        var snapshot = EquipmentShopDefaults.CreateSnapshot();
        var settings = snapshot.Settings.ToDictionary(entry => entry.Key, entry => entry.Value);
        settings[EquipmentShopType.Human] = settings[EquipmentShopType.Human] with
        {
            DisplayName = "Humans only",
            MaxPurchasesPerRound = 2
        };
        settings[EquipmentShopType.Zombie] = settings[EquipmentShopType.Zombie] with
        {
            DisplayName = "Zombies only",
            MaxPurchasesPerMap = 5
        };

        catalog.Replace(snapshot with { Settings = settings });

        Assert.Equal("Humans only", catalog.GetSettings(EquipmentShopType.Human).DisplayName);
        Assert.Equal(2, catalog.GetSettings(EquipmentShopType.Human).MaxPurchasesPerRound);
        Assert.Equal("Zombies only", catalog.GetSettings(EquipmentShopType.Zombie).DisplayName);
        Assert.Equal(5, catalog.GetSettings(EquipmentShopType.Zombie).MaxPurchasesPerMap);
    }

    [Fact]
    public void Replace_RejectsMissingStorefrontWithoutChangingCurrentSnapshot()
    {
        var catalog = new EquipmentShopRuntimeCatalog();
        var originalHuman = catalog.GetSettings(EquipmentShopType.Human);
        var snapshot = EquipmentShopDefaults.CreateSnapshot();
        var incompleteSettings = snapshot.Settings
            .Where(entry => entry.Key != EquipmentShopType.Zombie)
            .ToDictionary(entry => entry.Key, entry => entry.Value);

        Assert.Throws<InvalidOperationException>(() =>
            catalog.Replace(snapshot with { Settings = incompleteSettings })
        );
        Assert.Same(originalHuman, catalog.GetSettings(EquipmentShopType.Human));
    }
}
