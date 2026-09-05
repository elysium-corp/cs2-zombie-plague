using System.ComponentModel.DataAnnotations;
using SupplyBox.Configuration;
using SupplyBox.Data.Configs;
using Xunit;

namespace SupplyBox.Tests;

public sealed class ConfigurationTests
{
    [Fact]
    public void ExportedFallbackRestoresMapCoordinatesAndRewardAmounts()
    {
        var document = SupplyBoxDocument.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fallback.json")));
        document.Maps.Add(new() { Name = "workshop/123/zm_test", Points = [new() { Id = 1, X = -350.5, Y = 100, Z = 64, Yaw = 90, BoxType = "standard" }] });
        document.BoxTypes[0].Loot[0].MaxAmount = 700;
        var copy = document.Clone();
        Assert.Equal(-350.5, copy.Maps[0].Points[0].X);
        Assert.Equal(700, copy.BoxTypes[0].Loot[0].MaxAmount);
        copy.Maps[0].Points.Clear();
        Assert.Single(document.Maps[0].Points);
    }

    [Fact]
    public void CrossMapPointIdsAreAllowedButDuplicatesWithinAMapAreRejected()
    {
        var document = new SupplyBoxDocument { Maps = [new() { Name = "de_mirage", Points = [new() { Id = 1 }] }, new() { Name = "de_dust2", Points = [new() { Id = 1 }] }] };
        document.Validate();
        document.Maps[0].Points.Add(new() { Id = 1 });
        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Theory]
    [InlineData("weapon_c4")]
    [InlineData("weapon_hegrenade")]
    [InlineData("prop_dynamic")]
    public void WeaponLootCannotSpawnArbitraryEntities(string item)
    {
        var document = new SupplyBoxDocument();
        document.BoxTypes[0].Loot = [new() { Kind = "weapon", ItemKey = item, MinAmount = 1, MaxAmount = 1 }];
        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Fact]
    public void MissingBoxReferenceAndInvalidLootRangeAreRejected()
    {
        var document = new SupplyBoxDocument { Maps = [new() { Name = "de_mirage", Points = [new() { Id = 1, BoxType = "deleted" }] }] };
        Assert.Throws<InvalidDataException>(document.Validate);
        document.Maps.Clear(); document.BoxTypes[0].Loot[0].MinAmount = 500;
        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(32769)]
    public void InvalidCoordinatesAreRejected(double x)
    {
        var document = new SupplyBoxDocument { Maps = [new() { Name = "de_mirage", Points = [new() { Id = 1, X = x }] }] };
        Assert.Throws<ValidationException>(document.Validate);
    }

    [Fact]
    public void RoundScheduleStartsAtConfiguredRoundAndRespectsSpecialModes()
    {
        var settings = new SupplyBoxConfig { StartFromRound = 3, EveryNthRound = 2 };
        Assert.False(SupplyBoxRules.RoundAllows(settings, 1, false, false));
        Assert.True(SupplyBoxRules.RoundAllows(settings, 3, false, false));
        Assert.False(SupplyBoxRules.RoundAllows(settings, 4, false, false));
        Assert.True(SupplyBoxRules.RoundAllows(settings, 5, false, false));
        Assert.False(SupplyBoxRules.RoundAllows(settings, 5, true, false));
        settings.AllowSurvivorRound = true;
        Assert.True(SupplyBoxRules.RoundAllows(settings, 5, true, false));
    }

    [Fact]
    public void MapOverrideAndRoundMapBudgetsAllLimitWaves()
    {
        var settings = new SupplyBoxConfig { MaxCountTogether = 2, MaxDropsPerRound = 3, MaxDropsPerMap = 8 };
        var map = new SupplyBoxMap { Name = "de_mirage", MaxCountTogether = 4 };
        Assert.False(SupplyBoxRules.LimitReached(settings, map, 2, 2, 7));
        Assert.True(SupplyBoxRules.LimitReached(settings, map, 4, 2, 7));
        Assert.True(SupplyBoxRules.LimitReached(settings, map, 0, 3, 7));
        Assert.True(SupplyBoxRules.LimitReached(settings, map, 0, 0, 8));
    }

    [Fact]
    public void PopulationRequiresEachConfiguredGroup()
    {
        var settings = new SupplyBoxConfig { MinPlayers = 4, MinAliveHumans = 2, MinAliveZombies = 1 };
        Assert.False(SupplyBoxRules.PopulationAllows(settings, 4, 1, 2));
        Assert.False(SupplyBoxRules.PopulationAllows(settings, 4, 3, 0));
        Assert.True(SupplyBoxRules.PopulationAllows(settings, 4, 2, 1));
    }
}
