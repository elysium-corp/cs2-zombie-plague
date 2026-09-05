using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using SupplyBox.Configuration;
using Xunit;

namespace SupplyBox.Tests;

public sealed class MapSettingsTests
{
    [Fact]
    public void LegacyDocumentMigratesAndMapOverridesDoNotMutateOtherMaps()
    {
        var document = new SupplyBoxDocument { SchemaVersion = 1, Maps = [
            new() { Name = "zm_a", ChanceDrop = 20, Overrides = new() { RespawnTimeBySeconds = 12, MinPlayers = 4, CountBots = true } },
            new() { Name = "zm_b" }
        ] };
        document.Validate();
        Assert.Equal(3, document.SchemaVersion);
        var a = document.ResolveSettings("ZM_A");
        Assert.Equal(20, a.ChanceDrop);
        Assert.Equal(12, a.RespawnTimeBySeconds);
        Assert.True(a.CountBots);
        Assert.False(SupplyBoxRules.PopulationAllows(a, 3, 2, 1));
        a.MinPlayers = 20;
        Assert.Equal(4, document.Maps[0].Overrides!.MinPlayers);
        Assert.Equal(120, document.ResolveSettings("zm_b").RespawnTimeBySeconds);
        Assert.Equal(100, document.Settings.ChanceDrop);
        Assert.Equal(120, document.ResolveSettings("missing").RespawnTimeBySeconds);
    }

    [Fact]
    public void ExplicitZeroAndFalseOverrideLegacyAndGlobalValues()
    {
        var map = new SupplyBoxMap { Name = "zm_test", ChanceDrop = 80, MaxCountTogether = 6,
            Overrides = new() { ChanceDrop = 0, MaxCountTogether = 1, MaxDropsPerRound = 0, HumansCanCollect = false } };
        var document = new SupplyBoxDocument { Maps = [map] };
        document.Settings.MaxDropsPerRound = 2;
        var settings = document.ResolveSettings(map.Name);
        Assert.Equal(0, settings.ChanceDrop);
        Assert.Equal(0, settings.MaxDropsPerRound);
        Assert.False(settings.HumansCanCollect);
        Assert.True(SupplyBoxRules.LimitReached(settings, map, 1, 0, 0));
        document.Settings.Enabled = false;
        Assert.False(document.ResolveSettings(map.Name).Enabled);
    }

    [Fact]
    public void FallbackRoundTripPreservesRadarAndEveryOverride()
    {
        var document = new SupplyBoxDocument { Maps = [new() { Name = "zm_gorodok_cs2_v1",
            Radar = new() { ImageId = new string('a', 64), ImageName = "radar.png", OverviewName = "zm_gorodok_cs2_v1", Calibrated = true, PosX = -3000, PosY = 3000, Scale = 11.719 },
            DefaultPointZ = 64, AllowedBoxTypes = ["standard"],
            Overrides = new() { DropHeight = 200, ParachuteSound = "test", EveryNthRound = 3 },
            Points = [new() { Id = 1, Z = 64 }] } ] };
        var copy = document.Clone();
        copy.Maps[0].Points[0].X = 500;
        copy = copy.Clone();
        Assert.Equal(JsonSerializer.Serialize(document.Maps[0].Radar), JsonSerializer.Serialize(copy.Maps[0].Radar));
        Assert.Equal(64, copy.Maps[0].DefaultPointZ);
        Assert.Equal(200, copy.ResolveSettings(copy.Maps[0].Name).DropHeight);
        Assert.Equal("standard", Assert.Single(copy.Maps[0].AllowedBoxTypes!));
        Assert.Equal(64, copy.Maps[0].Points[0].Z);
    }

    [Fact]
    public void AllowedTypesDistinguishInheritanceFromEmptySelection()
    {
        var document = new SupplyBoxDocument();
        document.BoxTypes.Add(new() { Key = "rare", Enabled = true });
        var map = new SupplyBoxMap { Name = "zm_test" };
        Assert.Equal(2, document.AvailableTypes(map).Count());
        map.AllowedBoxTypes = [];
        Assert.Empty(document.AvailableTypes(map));
        map.AllowedBoxTypes = ["rare"];
        Assert.Equal("rare", Assert.Single(document.AvailableTypes(map)).Key);
        document.BoxTypes[1].Enabled = false;
        Assert.Empty(document.AvailableTypes(map));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    public void InvalidRadarScaleIsRejected(double scale)
    {
        var document = new SupplyBoxDocument { Maps = [new() { Name = "zm_test", Radar = new() { Scale = scale } }] };
        Assert.Throws<ValidationException>(document.Validate);
    }

    [Fact]
    public void InvalidMapReferencesAndOverridesAreRejected()
    {
        var document = new SupplyBoxDocument { Maps = [new() { Name = "zm_test", AllowedBoxTypes = ["missing"] }] };
        Assert.Throws<InvalidDataException>(document.Validate);
        document.Maps[0].AllowedBoxTypes = ["standard", "standard"];
        Assert.Throws<InvalidDataException>(document.Validate);
        document.Maps[0].AllowedBoxTypes = null;
        document.Maps[0].Overrides = new() { EveryNthRound = 0 };
        Assert.Throws<ValidationException>(document.Validate);
        document.Maps[0].Overrides = null;
        document.Maps[0].Radar = new() { ImageId = "https://external/image.svg" };
        Assert.Throws<InvalidDataException>(document.Validate);
        document.Maps[0].Radar = null;
        document.SchemaVersion = 4;
        Assert.Throws<InvalidDataException>(document.Validate);
    }
}
