using System.Text.Json.Nodes;
using SupplyBox.Configuration;
using Xunit;

namespace SupplyBox.Tests;

public sealed class SoundEventsTests
{
    [Fact]
    public void MapOverridesInheritDisableOrReplaceWithoutMutatingGlobalList()
    {
        var document = new SupplyBoxDocument();
        document.Settings.DropSoundEvents = ["Drop.Global"];
        var map = new SupplyBoxMap { Name = "zm_test", Overrides = new() };
        document.Maps.Add(map);
        var settings = document.ResolveSettings(map.Name);
        Assert.Equal(["Drop.Global"], settings.DropSoundEvents);
        settings.DropSoundEvents.Clear();
        Assert.Single(document.Settings.DropSoundEvents);
        map.Overrides.DropSoundEvents = [];
        Assert.Empty(document.ResolveSettings(map.Name).DropSoundEvents);
        map.Overrides.DropSoundEvents = ["Drop.Map"];
        Assert.Equal(["Drop.Map"], document.ResolveSettings(map.Name).DropSoundEvents);
        document.BoxTypes[0].DropSoundEvents = [];
        var copy = document.Clone();
        Assert.Empty(copy.BoxTypes[0].DropSoundEvents!);
        Assert.Equal(["Drop.Map"], copy.Maps[0].Overrides!.DropSoundEvents);
    }

    [Fact]
    public void RandomSelectionUsesEveryConfiguredEventAndEmptyListIsSilent()
    {
        string[] events = ["Drop.One", "Drop.Two", "Drop.Three"];
        for (var index = 0; index < events.Length; index++)
        {
            var selectedIndex = index;
            Assert.Equal(events[index], SupplyBoxSoundEvents.Choose(events, count =>
            {
                Assert.Equal(events.Length, count);
                return selectedIndex;
            }));
        }
        Assert.Null(SupplyBoxSoundEvents.Choose([], _ => throw new InvalidOperationException()));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void OlderFallbacksMigrateToSilentDefaults(int version)
    {
        var node = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "fallback.json")))!;
        node["SchemaVersion"] = version;
        node["Settings"]!.AsObject().Remove("DropSoundEvents");
        node["Settings"]!["AutoDiscoverSpawnPoints"] = true;
        node["BoxTypes"]![0]!.AsObject().Remove("DropSoundEvents");
        var document = SupplyBoxDocument.Parse(node.ToJsonString());
        Assert.Equal(3, document.SchemaVersion);
        Assert.Empty(document.Settings.DropSoundEvents);
        Assert.Null(document.BoxTypes[0].DropSoundEvents);
        Assert.Empty(document.Maps);
    }

    [Theory]
    [InlineData("")]
    [InlineData("sound event")]
    [InlineData("Sound;command")]
    [InlineData("Sound\n")]
    [InlineData("Звук")]
    public void InvalidEventNamesAreRejectedAtEveryLevel(string name)
    {
        var document = new SupplyBoxDocument();
        document.Settings.DropSoundEvents = [name];
        Assert.Throws<InvalidDataException>(document.Validate);
        document.Settings.DropSoundEvents = [];
        document.BoxTypes[0].DropSoundEvents = [name];
        Assert.Throws<InvalidDataException>(document.Validate);
        document.BoxTypes[0].DropSoundEvents = null;
        document.Maps.Add(new() { Name = "zm_test", Overrides = new() { DropSoundEvents = [name] } });
        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Fact]
    public void ListsRejectDuplicatesOversizeAndNullGlobalValue()
    {
        Assert.Throws<InvalidDataException>(() => SupplyBoxSoundEvents.Validate(["Drop.One", "drop.one"]));
        Assert.Throws<InvalidDataException>(() => SupplyBoxSoundEvents.Validate([new string('a', 129)]));
        Assert.Throws<InvalidDataException>(() => SupplyBoxSoundEvents.Validate(Enumerable.Range(0, 17).Select(i => $"Drop.{i}").ToArray()));
        var document = new SupplyBoxDocument();
        document.Settings.DropSoundEvents = null!;
        Assert.Throws<InvalidDataException>(document.Validate);
        SupplyBoxSoundEvents.Validate(Enumerable.Range(0, 16).Select(i => $"Drop.{i}").ToArray());
    }
}
