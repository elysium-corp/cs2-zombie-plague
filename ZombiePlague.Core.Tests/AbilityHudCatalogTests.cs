using System.Text.Json;
using Xunit;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Human;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Entities;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Human.Classes;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Entities.Zombie.Classes;
using ZombiePlague.Core.Hud.AbilityHud;

namespace ZombiePlague.Core.Tests;

public sealed class AbilityHudCatalogTests
{
    private static string AsKey(string key) => key;
    private static AbilityFactory Factory() => new(null!, null!, () => throw new InvalidOperationException());
    private static ZombieCatalogDocument Example() => ZombieCatalogDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "zombie_catalog.example.json")));

    [Theory]
    [InlineData("human", new[] { "heal", "charge", "double_jump" })]
    [InlineData("survivor", new[] { "heal", "charge", "double_jump" })]
    [InlineData("zombie", new[] { "heal", "catch" })]
    [InlineData("nemesis", new[] { "heal", "catch" })]
    public void RoleShowsOnlyEnabledClassAndPersonalAbilitiesForItsSide(string kind, string[] expected)
    {
        const ulong steamId = 76561198000000001;
        var document = Example();
        document.Abilities.Single(item => item.InternalName == "heal").Side = "both";
        document.Abilities.Single(item => item.InternalName == "charge").Side = "human";
        document.Abilities.Single(item => item.InternalName == "catch").Side = "zombie";
        document.Abilities.Single(item => item.InternalName == "blind").Enabled = false;
        document.PlayerAbilities.Add(new() { SteamId = steamId.ToString(), Abilities = ["heal", "double_jump", "charge", "blind"] });
        document.PlayerAbilities.Add(new() { SteamId = "76561198000000002", Abilities = ["trap"] });
        var definition = document.Classes.First(item => item.Kind == kind);
        definition.Abilities = ["heal", "double_jump", "catch"];
        document.Validate();
        var snapshot = new ZombieCatalogState(document, 1, "test");
        var abilities = snapshot.ResolveAbilities(definition.Abilities, steamId,
            kind is "human" or "survivor" ? AbilitySide.Human : AbilitySide.Zombie).Select(Factory().Create).ToList();
        IPlayerRole role = kind switch
        {
            "human" => Human.Create(null!, null!, new HMercenary(definition, abilities)),
            "survivor" => Human.Create(null!, null!, new HSurvivor(definition, abilities)),
            "nemesis" => Zombie.Create(null!, null!, new ZNemesis(definition, abilities)),
            _ => Zombie.Create(null!, null!, new ZCatalogClass(definition, abilities))
        };
        Assert.Equal(expected, AbilityHudFrame.ForRole(role, AsKey, true).Icons.Select(icon => icon.Key));
        // Новый каталог не превращает HUD действующей роли в предпросмотр будущих выдач
        document.Abilities.Clear();
        Assert.Equal(expected, AbilityHudFrame.ForRole(role, AsKey, true).Icons.Select(icon => icon.Key));
    }

    [Fact]
    public void EmptyHumanRoleDoesNotAcquireAbilitiesFromTheCatalog()
    {
        var role = Human.Create(null!, null!, new HMercenary(new HClassConfig().Mercenary, []));
        Assert.Empty(AbilityHudFrame.ForRole(role, AsKey, true).Icons);
    }

    [Fact]
    public void WholeMaximumAssignmentFitsWithoutTruncationOrCatalogExtras()
    {
        var document = Example();
        var source = document.Abilities.Single(item => item.InternalName == "heal");
        var keys = Enumerable.Range(0, 48).Select(index => "assigned_" + index).ToArray();
        foreach (var key in keys)
        {
            var definition = JsonSerializer.Deserialize<ZombieAbilityDefinition>(JsonSerializer.Serialize(source))!;
            definition.InternalName = key;
            document.Abilities.Add(definition);
        }
        var selected = document.Classes.First(item => item.Kind == "zombie");
        selected.Abilities = keys.Take(16).ToList();
        document.PlayerAbilities.Add(new() { SteamId = "76561198000000001", Abilities = keys.Skip(16).ToList() });
        document.Validate();
        var snapshot = new ZombieCatalogState(document, 1, "test");
        var owned = snapshot.ResolveAbilities(selected.Abilities, 76561198000000001, AbilitySide.Zombie).Select(Factory().Create);
        var frame = AbilityHudFrame.Create(owned, AsKey, true);
        Assert.Equal(keys, frame.Icons.Select(icon => icon.Key));
        Assert.Equal(6, frame.RowCount);
        var sink = new RecordingSink();
        var presenter = new AbilityHudPresenter(sink);
        presenter.Render(1, frame);
        Assert.Contains("Buff47:Shown:True", sink.Calls);
        Assert.Contains("BuffRow5:Shown:True", sink.Calls);
        sink.Calls.Clear();
        presenter.Render(1, new([frame.Icons[0]], true));
        Assert.Contains("Buff47:Shown:False", sink.Calls);
        Assert.Contains("BuffRow5:Shown:False", sink.Calls);
        Assert.DoesNotContain("BuffRow0:Shown:False", sink.Calls);
    }

    private sealed class RecordingSink : IAbilityHudSink
    {
        public List<string> Calls { get; } = [];
        public void SetClass(int playerId, string panel, string name, bool enabled) => Calls.Add($"{panel}:{name}:{enabled}");
        public void SetText(int playerId, string panel, string value) { }
    }
}
