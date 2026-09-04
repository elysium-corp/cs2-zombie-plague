using System.Text.Json;
using Shop.Api.Data;
using Shop.Core.Configuration;
using Shop.Core.Data;

namespace Shop.Core.Tests;

public sealed class ShopSnapshotMapperTests
{
    [Fact]
    public void DistributedFallbackTemplateBuildsAValidEmptySnapshot()
    {
        var json = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "template.jsonc"));
        var configuration = JsonSerializer.Deserialize<ShopFallbackConfig>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        });

        var snapshot = ShopSnapshotMapper.FromFallback(Assert.IsType<ShopFallbackConfig>(configuration));

        Assert.Equal("shop.json", snapshot.Source);
        Assert.Equal(2, snapshot.Storefronts.Count);
        Assert.Empty(snapshot.Categories);
        Assert.Empty(snapshot.Offers);
    }

    [Fact]
    public void FromFallbackBuildsRootAndCategorizedOffers()
    {
        var configuration = ValidConfiguration();
        configuration.Categories.Add(new ShopFallbackCategory
        {
            Id = 11,
            ShopType = "human",
            Key = "weapons",
            DisplayNameKey = "Shop.Category.Weapons",
            SortOrder = 10
        });
        configuration.Offers.Add(Offer(21, "human", 11));
        configuration.Offers.Add(Offer(22, "zombie", null));

        var snapshot = ShopSnapshotMapper.FromFallback(configuration);

        Assert.Equal("shop.json", snapshot.Source);
        Assert.Equal(2, snapshot.Storefronts.Count);
        Assert.Equal(2, snapshot.Offers.Count);
        Assert.Equal(11, snapshot.Offers.Single(item => item.Id == 21).CategoryId);
        Assert.Null(snapshot.Offers.Single(item => item.Id == 22).CategoryId);
    }

    [Fact]
    public void FromFallbackRejectsCategoryFromAnotherStorefront()
    {
        var configuration = ValidConfiguration();
        configuration.Categories.Add(new ShopFallbackCategory
        {
            Id = 11,
            ShopType = "zombie",
            Key = "mutations",
            DisplayNameKey = "Shop.Category.Mutations"
        });
        configuration.Offers.Add(Offer(21, "human", 11));

        Assert.Throws<InvalidDataException>(() => ShopSnapshotMapper.FromFallback(configuration));
    }

    [Theory]
    [InlineData("any")]
    [InlineData("all")]
    public void FromFallbackRejectsRestrictedOfferWithoutPrivileges(string accessMode)
    {
        var configuration = ValidConfiguration();
        var offer = Offer(21, "human", null);
        offer.AccessMode = accessMode;
        configuration.Offers.Add(offer);

        Assert.Throws<InvalidDataException>(() => ShopSnapshotMapper.FromFallback(configuration));
    }

    [Fact]
    public void FromFallbackNormalizesPrivilegeKeys()
    {
        var configuration = ValidConfiguration();
        var offer = Offer(21, "human", null);
        offer.AccessMode = "any";
        offer.RequiredPrivileges = ["VIP.Shop", "vip.shop"];
        configuration.Offers.Add(offer);

        var snapshot = ShopSnapshotMapper.FromFallback(configuration);

        var contract = Assert.Single(snapshot.Offers).Contract;
        Assert.Equal(ShopAccessMode.Any, contract.AccessMode);
        Assert.Equal("vip.shop", Assert.Single(contract.RequiredPrivileges));
    }

    [Fact]
    public void FromFallbackRejectsNonPositiveCategoryId()
    {
        var configuration = ValidConfiguration();
        configuration.Categories.Add(new ShopFallbackCategory
        {
            Id = 0,
            ShopType = "human",
            Key = "weapons",
            DisplayNameKey = "Shop.Category.Weapons"
        });

        Assert.Throws<InvalidDataException>(() => ShopSnapshotMapper.FromFallback(configuration));
    }

    [Fact]
    public void FromFallbackRejectsDuplicateProductInStorefront()
    {
        var configuration = ValidConfiguration();
        configuration.Offers.Add(Offer(21, "human", null));
        var duplicate = Offer(22, "human", null);
        duplicate.ItemKey = "WEAPON_21";
        configuration.Offers.Add(duplicate);

        Assert.Throws<InvalidDataException>(() => ShopSnapshotMapper.FromFallback(configuration));
    }

    [Fact]
    public void FromFallbackRejectsCategoryKeyWithLeadingSeparator()
    {
        var configuration = ValidConfiguration();
        configuration.Categories.Add(new ShopFallbackCategory
        {
            Id = 11,
            ShopType = "human",
            Key = "_weapons",
            DisplayNameKey = "Shop.Category.Weapons"
        });

        Assert.Throws<InvalidDataException>(() => ShopSnapshotMapper.FromFallback(configuration));
    }

    [Fact]
    public void FromFallbackRejectsInvalidArmorAmount()
    {
        var configuration = ValidConfiguration();
        var offer = Offer(21, "human", null);
        offer.ProviderKey = "builtin";
        offer.ItemKey = "armor";
        offer.SettingsJson = "{\"armor_amount\":101}";
        configuration.Offers.Add(offer);

        Assert.Throws<InvalidDataException>(() => ShopSnapshotMapper.FromFallback(configuration));
    }

    [Fact]
    public void FromFallbackRejectsNonCanonicalLocalizationKey()
    {
        var configuration = ValidConfiguration();
        var offer = Offer(21, "human", null);
        offer.DisplayNameKey = "equipment.weapon_21.name";
        configuration.Offers.Add(offer);

        Assert.Throws<InvalidDataException>(() => ShopSnapshotMapper.FromFallback(configuration));
    }

    private static ShopFallbackConfig ValidConfiguration() => new()
    {
        SchemaVersion = 1,
        GeneratedAt = new DateTimeOffset(2026, 9, 4, 0, 0, 0, TimeSpan.Zero),
        Storefronts =
        [
            new ShopFallbackStorefront
            {
                ShopType = "human",
                TitleKey = "Shop.Human.Title",
                SortMode = "priority"
            },
            new ShopFallbackStorefront
            {
                ShopType = "zombie",
                TitleKey = "Shop.Zombie.Title",
                SortMode = "alphabetical"
            }
        ]
    };

    private static ShopFallbackOffer Offer(long id, string shopType, long? categoryId) => new()
    {
        Id = id,
        ShopType = shopType,
        ProviderKey = "custom_equipment",
        ItemKey = $"weapon_{id}",
        DisplayNameKey = $"Equipment.Weapon.{id}.Name",
        CategoryId = categoryId,
        Price = 100,
        AmmoAmount = 30,
        SettingsJson = "{}"
    };
}
