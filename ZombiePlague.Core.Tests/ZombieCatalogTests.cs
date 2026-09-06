using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftlyS2.Shared;
using System.Text.Json.Nodes;
using Xunit;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities;

namespace ZombiePlague.Core.Tests;

public sealed class ZombieCatalogTests
{
    private static ZombieCatalogDocument Example() => ZombieCatalogDocument.Parse(
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "zombie_catalog.example.json")));

    [Fact]
    public void ExportedCatalogRoundTripsEveryClassAndAbility()
    {
        var document = Example();
        var copy = ZombieCatalogDocument.Parse(JsonSerializer.Serialize(document, ZombieCatalogDocument.JsonOptions));
        Assert.Equal(6, copy.Classes.Count);
        Assert.Equal(7, copy.Abilities.Count);
        Assert.Contains(copy.Resources(), path => path.EndsWith(".vmdl"));
        Assert.Contains(copy.Resources(), path => path.EndsWith(".vpcf"));
    }

    [Fact]
    public void NewAbilityIdUsesItsOwnParameters()
    {
        var document = Example();
        document.Abilities.Add(new()
        {
            InternalName = "medic_elite", Kind = "heal", DisplayName = "Медик",
            Parameters = JsonSerializer.SerializeToElement(new { HealAmount = 1750, CooldownTime = 42 })
        });
        document.Classes[0].Abilities = ["medic_elite", "leap"];
        document.Validate();
        var parameters = Assert.IsType<HealConfig>(AbilityParameters.Parse(document.Abilities[^1]));
        Assert.Equal(1750, parameters.HealAmount);
        Assert.Equal(42, parameters.CooldownTime);
        Assert.Equal(500, Assert.IsType<HealConfig>(AbilityParameters.Parse(document.Abilities[0])).HealAmount);
    }

    [Theory]
    [InlineData("missing_ability")]
    [InlineData("HEAL")]
    public void DanglingAbilityReferencesAreRejected(string key)
    {
        var document = Example();
        document.Classes[0].Abilities = [key];
        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Theory]
    [InlineData("charge", "SpeedUpdatePerTimeTick", 0)]
    [InlineData("charge", "ChargeTime", 0)]
    [InlineData("catch", "RedColorEffect", 256)]
    [InlineData("leap", "CooldownTime", -1)]
    public void UnsafeAbilityParametersAreRejected(string kind, string field, double value)
    {
        var document = Example();
        var ability = document.Abilities.Single(item => item.Kind == kind);
        var parameters = JsonNode.Parse(ability.Parameters.GetRawText())!;
        parameters[field] = value;
        ability.Parameters = JsonSerializer.SerializeToElement(parameters);
        Assert.ThrowsAny<Exception>(document.Validate);
    }

    [Fact]
    public void MultipleAbilitiesAreAllowedButDisabledDefaultIsRejected()
    {
        var document = Example();
        document.Classes[0].Abilities = ["heal", "catch"];
        document.Validate();
        document = Example();
        document.Classes.Single(item => item.InternalName == document.DefaultClass).Enabled = false;
        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Fact]
    public void PersonalAndClassAbilitiesHaveOneInstancePerKeyAndIndependentPlayerState()
    {
        const ulong steamId = 76561198000000001;
        var document = Example();
        document.PlayerAbilities.Add(new() { SteamId = steamId.ToString(), Abilities = ["heal", "catch"] });
        document.Validate();
        var snapshot = new ZombieCatalogState(document, 1, "database");
        var factory = new AbilityFactory(null!, null!, () => throw new InvalidOperationException());
        var abilities = snapshot.ResolveAbilities(["heal", "leap"], steamId, AbilitySide.Zombie).Select(factory.Create).ToArray();
        Assert.Collection(abilities, item => Assert.IsType<Heal>(item), item => Assert.IsType<Leap>(item), item => Assert.IsType<Catch>(item));
        var nextRole = snapshot.ResolveAbilities(["heal"], steamId, AbilitySide.Zombie).Select(factory.Create).ToArray();
        Assert.NotSame(abilities[0], nextRole[0]);
        Assert.Equal(["heal"], snapshot.ResolveAbilities(["heal"], steamId + 1, AbilitySide.Zombie).Select(item => item.InternalName));
    }

    [Fact]
    public void RevokingPersonalAssignmentsPreservesClassAbilities()
    {
        const ulong steamId = 76561198000000001;
        var document = Example();
        document.PlayerAbilities.Add(new() { SteamId = steamId.ToString(), Abilities = ["heal", "catch"], Enabled = false });
        var snapshot = new ZombieCatalogState(document, 2, "database");
        Assert.Equal(["heal"], snapshot.ResolveAbilities(["heal"], steamId, AbilitySide.Zombie).Select(item => item.InternalName));
        document.PlayerAbilities.Clear();
        snapshot = new(document, 3, "database");
        Assert.Equal(["heal"], snapshot.ResolveAbilities(["heal"], steamId, AbilitySide.Zombie).Select(item => item.InternalName));
    }

    [Fact]
    public void RoleSideFiltersClassAndPersonalAbilitiesBeforeDeduplicationCreatesInstances()
    {
        const ulong steamId = 76561198000000001;
        var document = Example();
        document.Abilities.Single(item => item.InternalName == "leap").Side = "both";
        document.Abilities.Single(item => item.InternalName == "catch").Enabled = false;
        document.PlayerAbilities.Add(new() { SteamId = steamId.ToString(), Abilities = ["heal", "leap", "double_jump", "catch"] });
        document.Validate();
        var snapshot = new ZombieCatalogState(document, 4, "database");
        string[] inherited = ["double_jump", "heal", "leap"];
        // Тот же SteamID получает новый набор при заражении и снова при превращении в человека
        var humans = snapshot.ResolveAbilities(inherited, steamId, AbilitySide.Human).Select(item => item.InternalName).ToArray();
        Assert.Equal(["double_jump", "leap"], humans);
        Assert.Equal(["heal", "leap"], snapshot.ResolveAbilities(inherited, steamId, AbilitySide.Zombie).Select(item => item.InternalName));
        Assert.Equal(humans, snapshot.ResolveAbilities(inherited, steamId, AbilitySide.Human).Select(item => item.InternalName));
    }

    [Theory]
    [InlineData("76561198000000000", false)]
    [InlineData("76561202255233023", false)]
    [InlineData("76561202255233024", true)]
    [InlineData("7656119800000000", true)]
    [InlineData("7656119800000000x", true)]
    public void PersonalSteamIdsKeepTheirFullPrecision(string steamId, bool invalid)
    {
        var document = Example();
        document.PlayerAbilities.Add(new() { SteamId = steamId, Abilities = ["heal"] });
        if (invalid) Assert.Throws<InvalidDataException>(document.Validate);
        else
        {
            var roundTrip = ZombieCatalogDocument.Parse(JsonSerializer.Serialize(document, ZombieCatalogDocument.JsonOptions));
            Assert.Equal(steamId, roundTrip.PlayerAbilities[0].SteamId);
        }
    }

    [Fact]
    public void InvalidSidesAndDuplicateOrDanglingPersonalReferencesAreRejected()
    {
        var document = Example();
        document.Abilities[0].Side = "spectator";
        Assert.Throws<InvalidDataException>(document.Validate);
        document = Example();
        var assignment = new PlayerAbilityAssignment { SteamId = "76561198000000001", Abilities = ["heal", "heal"] };
        document.PlayerAbilities.Add(assignment);
        Assert.Throws<InvalidDataException>(document.Validate);
        assignment.Abilities = ["deleted"];
        Assert.Throws<InvalidDataException>(document.Validate);
        assignment.Abilities = ["heal"];
        document.PlayerAbilities.Add(assignment);
        Assert.Throws<InvalidDataException>(document.Validate);
    }

    [Fact]
    public async Task DatabaseWinsWithoutReadingFallback()
    {
        var expected = new ZombieCatalogState(Example(), 9, "database");
        var state = await ZombieCatalogLoader.LoadAsync(_ => Task.FromResult(expected),
            _ => throw new Exception("Fallback не должен читаться"), null, CancellationToken.None);
        Assert.Same(expected, state);
    }

    [Fact]
    public async Task EachDatabaseFailureReadsFreshFallbackAndDatabaseRecoveryReplacesIt()
    {
        var fallback = Example();
        var readCount = 0;
        Task<ZombieCatalogState> Offline(CancellationToken _) => throw new IOException("offline");
        Task<ZombieCatalogDocument> Read(CancellationToken _) { readCount++; return Task.FromResult(fallback); }
        var state = await ZombieCatalogLoader.LoadAsync(Offline, Read, null, CancellationToken.None);
        Assert.Equal("fallback", state.Source);
        fallback = Example();
        fallback.Classes[0].Health = 9999;
        state = await ZombieCatalogLoader.LoadAsync(Offline, Read, state, CancellationToken.None);
        Assert.Equal(2, readCount);
        Assert.Equal(9999, state.Document.Classes[0].Health);
        state = await ZombieCatalogLoader.LoadAsync(_ => Task.FromResult(new ZombieCatalogState(Example(), 10, "database")), Read, state, CancellationToken.None);
        Assert.Equal("database", state.Source);
        Assert.Equal(10, state.Version);
        Assert.Equal(2, readCount);
    }

    [Fact]
    public async Task BrokenDatabaseAndFallbackKeepWholePreviousSnapshot()
    {
        var previous = new ZombieCatalogState(Example(), 12, "database");
        var broken = Example();
        broken.Abilities.Clear();
        var state = await ZombieCatalogLoader.LoadAsync(_ => Task.FromResult(new ZombieCatalogState(broken, 13, "database")),
            _ => throw new JsonException("broken fallback"), previous, CancellationToken.None);
        Assert.Same(previous.Document, state.Document);
        Assert.Equal(12, state.Version);
        Assert.Equal("memory", state.Source);
        Assert.NotNull(state.DatabaseError);
        Assert.NotNull(state.FallbackError);
    }

    [Fact]
    public async Task CancellationNeverActivatesFallback()
    {
        using var stop = new CancellationTokenSource();
        await Assert.ThrowsAsync<OperationCanceledException>(() => ZombieCatalogLoader.LoadAsync(_ =>
        {
            stop.Cancel();
            stop.Token.ThrowIfCancellationRequested();
            throw new Exception();
        }, _ => throw new Exception("Fallback не должен читаться"), null, stop.Token));
    }

    [Fact]
    public async Task ReloadStagesNewResourcesUntilMapPrecacheAndThenAppliesFreshFallback()
    {
        var directory = Path.Combine(Path.GetTempPath(), "zombie-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var path = Path.Combine(directory, "zombie_catalog.json");
            var document = Example();
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(document, ZombieCatalogDocument.JsonOptions));
            var core = (ISwiftlyCore)DispatchProxy.Create(typeof(ISwiftlyCore), typeof(CatalogCoreStub));
            ((CatalogCoreStub)core).Handler = method => method.Name switch
            {
                "get_Database" => throw new IOException("offline"),
                "get_Logger" => NullLogger.Instance,
                "get_Configuration" => ConfigurationStub(method.ReturnType, directory),
                _ => throw new InvalidOperationException(method.Name)
            };
            using var service = new ZombieCatalogService(core, new ZombieCatalogRepository(core));
            service.Initialize();
            Assert.Equal("fallback", service.Current.Source);
            var originalModel = service.Current.Document.Classes[0].Model;
            Assert.Contains(originalModel, service.PrepareResourcesForMap());
            document.Classes[0].Model = "characters/models/new_zombie.vmdl";
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(document, ZombieCatalogDocument.JsonOptions));
            await service.RefreshAsync();
            Assert.Equal(originalModel, service.Current.Document.Classes[0].Model);
            Assert.Equal(0, service.PendingResourceVersion);
            Assert.Contains(document.Classes[0].Model, service.PrepareResourcesForMap());
            Assert.Equal(document.Classes[0].Model, service.Current.Document.Classes[0].Model);
            Assert.Null(service.PendingResourceVersion);
            document.Classes[0].Health = 8765;
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(document, ZombieCatalogDocument.JsonOptions));
            await service.RefreshAsync();
            Assert.Equal(8765, service.Current.Document.Classes[0].Health);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static object ConfigurationStub(Type type, string directory)
    {
        var proxy = DispatchProxy.Create(type, typeof(CatalogCoreStub));
        ((CatalogCoreStub)proxy).Handler = method => method.Name == "get_BasePath"
            ? directory : throw new InvalidOperationException(method.Name);
        return proxy;
    }

    public class CatalogCoreStub : DispatchProxy
    {
        public Func<MethodInfo, object?> Handler { get; set; } = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args) => Handler(targetMethod!);
    }

    [Fact]
    public void LegacyMigrationKeepsCustomIdsValuesAndSharedAbilitySettings()
    {
        var directory = Path.Combine(Path.GetTempPath(), "zombie-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var document = Example();
            var classes = new JsonObject();
            foreach (var definition in document.Classes)
            {
                var item = JsonSerializer.SerializeToNode(definition)!.AsObject();
                foreach (var field in new[] { "Kind", "SortOrder", "PreviewModel", "DisplayNameKey", "DescriptionKey" }) item.Remove(field);
                if (definition.Kind != "nemesis") item["Health"] = 4321;
                classes[definition.Kind == "nemesis" ? "Nemesis" : definition.InternalName] = item;
            }
            var abilities = new JsonObject();
            foreach (var definition in document.Abilities)
            {
                var item = JsonNode.Parse(definition.Parameters.GetRawText())!.AsObject();
                item["Enable"] = definition.Enabled;
                abilities[definition.Kind == "double_jump" ? "DoubleJump" : definition.Kind] = item;
            }
            File.WriteAllText(Path.Combine(directory, "zombie_class.json"), new JsonObject { ["ZClassConfig"] = classes }.ToJsonString());
            File.WriteAllText(Path.Combine(directory, "ability.json"), new JsonObject { ["AbilityConfig"] = abilities }.ToJsonString());
            var migrated = Assert.IsType<ZombieCatalogDocument>(LegacyZombieCatalog.TryRead(directory));
            Assert.Equal(4321, migrated.Classes[0].Health);
            Assert.Contains(migrated.Abilities, item => item.InternalName == "double_jump");
            Assert.Equal("zombie_nemesis", migrated.NemesisClass);
            Assert.True(File.Exists(Path.Combine(directory, "zombie_class.json")));
        }
        finally { Directory.Delete(directory, true); }
    }
}
