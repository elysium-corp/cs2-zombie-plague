using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using Xunit;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Human;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Entities.Human.Classes;
using ZombiePlague.Core.Data.Entities.Human.Factory;

namespace ZombiePlague.Core.Tests;

/// <summary>Проверки переноса человеческих классов и их игрового применения</summary>
public sealed class HumanCatalogTests
{
    private static ZombieCatalogDocument Example() => ZombieCatalogDocument.Parse(File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory, "zombie_catalog.example.json")));

    /// <summary>Перенос сохраняет индивидуальные параметры и выполняется один раз</summary>
    [Fact]
    public void LegacyUpgradePreservesHumanStatsAndReferencesAndIsIdempotent()
    {
        var document = Example();
        document.FormatVersion = 1;
        document.Classes.RemoveAll(item => item.Kind is "human" or "survivor");
        var humans = new HClassConfig();
        humans.Mercenary.InternalName = "human_medic";
        humans.Mercenary.Health = 185;
        humans.Mercenary.Armor = 72;
        humans.Mercenary.Abilities = ["double_jump", "heal", "double_jump", "deleted"];
        CatalogUpgrade.Apply(document, humans);
        document.Validate();
        var medic = document.Classes.Single(item => item.InternalName == "human_medic");
        Assert.Equal(185, medic.Health);
        Assert.Equal(72, medic.Armor);
        Assert.Equal(["double_jump", "heal"], medic.Abilities);
        Assert.Equal("ZombiePlague.HClass.Human.Medic.Name", medic.DisplayNameKey);
        Assert.Equal("human_medic", document.DefaultHumanClass);
        CatalogUpgrade.Apply(document, new());
        Assert.Equal(8, document.Classes.Count);
        Assert.Equal(185, medic.Health);
    }

    /// <summary>Специальный режим не может стать обычным выбираемым классом</summary>
    [Theory]
    [InlineData("human_survivor")]
    [InlineData("zombie_cleric")]
    [InlineData("missing")]
    public void HumanFactoryUsesConfiguredDefaultForIneligibleSelections(string selection)
    {
        var document = Example();
        document.Classes.Single(item => item.Kind == "human").Health = 147;
        WithCatalog(document, (core, catalog, _) =>
        {
            var factory = new HClassFactory(catalog, new AbilityFactory(core, catalog, () => null!));
            var selected = factory.CreateOrDefault(selection);
            Assert.IsType<HMercenary>(selected);
            Assert.Equal(147, selected.Health);
            Assert.IsType<HSurvivor>(factory.Create<HSurvivor>());
        });
    }

    /// <summary>Произвольный человеческий класс использует параметры каталога и один экземпляр каждого ID</summary>
    [Fact]
    public void CustomHumanClassCombinesPersonalAbilitiesOnceAndFiltersTheirSide()
    {
        var document = Example();
        var medic = JsonSerializer.Deserialize<ZombieClassDefinition>(JsonSerializer.Serialize(document.Classes.Single(item => item.Kind == "human")))!;
        medic.InternalName = "human_medic";
        medic.Health = 163;
        medic.Armor = 45;
        medic.Abilities = ["heal", "double_jump", "catch"];
        document.Classes.Add(medic);
        document.Abilities.Single(item => item.InternalName == "heal").Side = "both";
        document.PlayerAbilities.Add(new() { SteamId = "76561198000000001", Abilities = ["heal", "double_jump"] });
        WithCatalog(document, (core, catalog, _) =>
        {
            var factory = new HClassFactory(catalog, new AbilityFactory(core, catalog, () => null!));
            var selected = factory.CreateOrDefault("human_medic", 76561198000000001);
            Assert.Equal(163, selected.Health);
            Assert.Equal(45, selected.Armor);
            Assert.Collection(selected.Abilities, item => Assert.IsType<Heal>(item), item => Assert.IsType<DoubleJump>(item));
            var nextRole = factory.CreateOrDefault("human_medic", 76561198000000001);
            Assert.NotSame(selected.Abilities[0], nextRole.Abilities[0]);
        });
    }

    /// <summary>Новый формат не обращается к старым испорченным файлам</summary>
    [Fact]
    public void VersionTwoFallbackIgnoresBrokenLegacyHumanConfig()
    {
        WithCatalog(Example(), (_, catalog, directory) =>
        {
            File.WriteAllText(Path.Combine(directory, "human_class.json"), "broken legacy");
            catalog.RefreshAsync().GetAwaiter().GetResult();
            Assert.Equal("fallback", catalog.Current.Source);
            Assert.Null(catalog.Current.FallbackError);
        });
    }

    /// <summary>Оба события начала карты выполняют один запрос независимо от их порядка</summary>
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void MapLoadAndPrecacheShareOneRefreshEvenAfterItCompleted(bool loadFirst)
    {
        WithCatalog(Example(), (core, catalog, _) =>
        {
            using var lifecycle = new ZombieCatalogLifecycle(core, catalog);
            var before = ((CoreStub)core).DatabaseReads;
            var precache = DispatchProxy.Create<IOnPrecacheResourceEvent, CoreStub>();
            ((CoreStub)precache).Handler = _ => null;
            void Invoke(string name, object? value) => typeof(ZombieCatalogLifecycle).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(lifecycle, [value]);
            Invoke(loadFirst ? "OnMapLoad" : "OnPrecache", loadFirst ? null : precache);
            var pending = (Task)typeof(ZombieCatalogLifecycle).GetField("_mapRefresh", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(lifecycle)!;
            pending.GetAwaiter().GetResult();
            Invoke(loadFirst ? "OnPrecache" : "OnMapLoad", loadFirst ? precache : null);
            Assert.Equal(before + 1, ((CoreStub)core).DatabaseReads);
            Invoke("OnMapUnload", null);
            Invoke("OnPrecache", precache);
            Assert.Equal(before + 2, ((CoreStub)core).DatabaseReads);
        });
    }

    /// <summary>Обязательные ключи локализации и типы классов проверяются до публикации</summary>
    [Fact]
    public void InvalidHumanDefaultsAndRawNamesWithoutKeysAreRejected()
    {
        var document = Example();
        document.DefaultHumanClass = document.SurvivorClass;
        Assert.Throws<InvalidDataException>(document.Validate);
        document = Example();
        document.Abilities[0].DisplayNameKey = "";
        document.Abilities[0].DisplayName = "Лечение";
        Assert.Throws<InvalidDataException>(document.Validate);
        document = Example();
        document.Classes.Single(item => item.Kind == "human").Armor = -1;
        Assert.Throws<InvalidDataException>(document.Validate);
        Assert.Contains("CREATE TABLE IF NOT EXISTS", ZombieCatalogRepository.ReadSql("schema.sql"));
    }

    private static void WithCatalog(ZombieCatalogDocument document, Action<ISwiftlyCore, ZombieCatalogService, string> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "human-catalog-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "zombie_catalog.json"), JsonSerializer.Serialize(document, ZombieCatalogDocument.JsonOptions));
            var core = DispatchProxy.Create<ISwiftlyCore, CoreStub>();
            ((CoreStub)core).Handler = method => method.Name switch
            {
                "get_Database" => throw new IOException("offline"),
                "get_Logger" => NullLogger.Instance,
                "get_Configuration" => Configuration(method.ReturnType, directory),
                _ => throw new InvalidOperationException(method.Name)
            };
            using var catalog = new ZombieCatalogService(core, new ZombieCatalogRepository(core));
            catalog.Initialize();
            action(core, catalog, directory);
        }
        finally { Directory.Delete(directory, true); }
    }

    private static object Configuration(Type type, string directory)
    {
        var proxy = DispatchProxy.Create(type, typeof(CoreStub));
        ((CoreStub)proxy).Handler = method => method.Name == "get_BasePath" ? directory : throw new InvalidOperationException(method.Name);
        return proxy;
    }

    /// <summary>Заглушка локального окружения без запуска игрового сервера</summary>
    public class CoreStub : DispatchProxy
    {
        /// <summary>Обработчик обращения к API</summary>
        public Func<MethodInfo, object?> Handler { get; set; } = null!;
        /// <summary>Число попыток обращения к БД</summary>
        public int DatabaseReads { get; private set; }
        /// <inheritdoc />
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name == "get_Database") DatabaseReads++;
            return Handler(targetMethod);
        }
    }
}
