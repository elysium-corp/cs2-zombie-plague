using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Shop.Api.Data;
using Shop.Core.Application;
using Shop.Core.Data;
using Shop.Core.Database;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Shop.Core.Tests;

public sealed class StandardWeaponTests
{
    [Fact]
    public void MigrationAndRuntimeHaveTheSame34FirearmsInTheCorrectSlots()
    {
        var options = new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=shop_migration_test;Username=test;Password=test")
            .Options;
        using var context = new ShopDbContext(options);
        var script = context.GetService<IMigrator>().GenerateScript(
            "20260904160000_CreateShopModule", "20260905060000_AddStandardWeaponsAndCategorizeOffers");
        var seeds = Regex.Matches(script,
            @"\('(weapon_[a-z0-9_]+)', 'Shop\.Weapon\.[^']+', '([^']+)', (\d+)\)");

        Assert.Equal(34, seeds.Count);
        Assert.Equal(StandardWeaponCatalog.Weapons.Keys.Order(),
            seeds.Select(match => match.Groups[1].Value).Order());
        Assert.Equal(10, seeds.Count(match => match.Groups[2].Value == "pistol"));
        Assert.Equal(7, seeds.Count(match => match.Groups[2].Value == "submachine_gun"));
        Assert.Equal(7, seeds.Count(match => match.Groups[2].Value == "rifle"));
        Assert.Equal(4, seeds.Count(match => match.Groups[2].Value == "sniper_rifle"));
        Assert.Equal(4, seeds.Count(match => match.Groups[2].Value == "shotgun"));
        Assert.Equal(2, seeds.Count(match => match.Groups[2].Value == "machine_gun"));

        foreach (Match seed in seeds)
        {
            Assert.Equal(seed.Groups[2].Value == "pistol"
                    ? gear_slot_t.GEAR_SLOT_PISTOL : gear_slot_t.GEAR_SLOT_RIFLE,
                StandardWeaponCatalog.Weapons[seed.Groups[1].Value]);
            Assert.True(int.Parse(seed.Groups[3].Value) > 0);
        }
    }

    [Theory]
    [InlineData("weapon_hegrenade")]
    [InlineData("weapon_flashbang")]
    [InlineData("weapon_smokegrenade")]
    [InlineData("weapon_molotov")]
    [InlineData("weapon_incgrenade")]
    [InlineData("weapon_decoy")]
    [InlineData("weapon_c4")]
    [InlineData("weapon_taser")]
    [InlineData("weapon_knife")]
    [InlineData("item_defuser")]
    [InlineData("item_kevlar")]
    [InlineData("weapon_unknown")]
    public void NonFirearmsAreRejectedBeforeAccessingPlayerOrEquipment(string itemKey)
    {
        var player = Player((_, _) => throw new InvalidOperationException("Выдача не ожидалась."));
        var provider = new ShopProductProvider(() => throw new InvalidOperationException());
        var offer = Offer(ShopType.Human, itemKey);

        Assert.False(provider.IsAvailable(player, offer));
        Assert.False(provider.TryGrant(player, offer));
    }

    [Fact]
    public void StandardWeaponsCannotBeGrantedThroughTheZombieStorefront()
    {
        var player = Player((_, _) => throw new InvalidOperationException("Выдача не ожидалась."));
        var provider = new ShopProductProvider(() => throw new InvalidOperationException());

        foreach (var itemKey in StandardWeaponCatalog.Weapons.Keys)
        {
            var offer = Offer(ShopType.Zombie, itemKey);
            Assert.False(provider.IsAvailable(player, offer));
            Assert.False(provider.TryGrant(player, offer));
        }
    }

    [Fact]
    public void MissingPawnRejectsTheGrantInsteadOfReportingAPurchase()
    {
        var player = Player((method, _) => method.Name switch
        {
            "get_IsValid" or "get_IsAlive" => true,
            "get_PlayerPawn" => null,
            _ => throw new InvalidOperationException(method.Name)
        });
        var provider = new ShopProductProvider(() => throw new InvalidOperationException());
        var offer = Offer(ShopType.Human, "weapon_ak47");

        Assert.False(provider.IsAvailable(player, offer));
        Assert.False(provider.TryGrant(player, offer));
    }

    private static ShopOfferDefinition Offer(ShopType shopType, string itemKey) => new(
        new ShopOffer(1, shopType, StandardWeaponCatalog.ProviderKey, itemKey,
            "Shop.Weapon.Ak47.Name", null, 2700, null, 1, 0, 0, 0,
            ShopAccessMode.Everyone, new HashSet<string>(), true, 0), null, "{}");

    private static IPlayer Player(Func<MethodInfo, object?[]?, object?> handler)
    {
        var player = DispatchProxy.Create<IPlayer, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)player).Handler = handler;
        return player;
    }
}
