using System.Reflection;
using Common.Database.Abstractions;
using Common.Database.Storages;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using SwiftlyS2.Shared.Menus;
using Xunit;
using ZombiePlague.Core.Data.Service;
using ZombiePlague.Core.Database;
using ZombiePlague.Core.Database.Entities;
using ZombiePlague.Core.Experimental.AbilityHud;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Tests;

public sealed class AbilityHudPreferencesTests
{
    [Fact]
    public void SettingsArePerSteamIdAndChangesSurviveLateDatabaseLoadIncludingReset()
    {
        var sessions = new PlayerSessionStore<PlayerPreferences>();
        var one = sessions.Create(1, new());
        var two = sessions.Create(2, new());
        var settings = new AbilityHudSettings(sessions);
        Assert.True(settings.Update(1, _ => new(50, "top_right")));
        Assert.Equal(new(50, "top_right"), settings.Get(1));
        Assert.Equal(AbilityHudPreferences.Default, settings.Get(2));
        var loaded = new PlayerPreferences { ZClassId = "zombie_hunter", AbilityHud = new(75, "middle_left") };
        one.CompleteLoadMerged(data => data.MergeLoaded(loaded));
        two.CompleteLoadMerged(data => data.MergeLoaded(loaded));
        Assert.Equal(new(50, "top_right"), settings.Get(1));
        Assert.Equal(loaded.AbilityHud, settings.Get(2));
        Assert.Equal("zombie_hunter", one.Read(data => data.ZClassId));

        var reset = sessions.Create(3, new());
        settings.Reset(3);
        reset.CompleteLoadMerged(data => data.MergeLoaded(loaded));
        Assert.Equal(AbilityHudPreferences.Default, settings.Get(3));
        Assert.True(reset.CreateSnapshot(data => data.Snapshot()).IsDirty);
        sessions.TryRemove(1, out _);
        Assert.False(settings.Update(1, _ => new(75, "top_left")));
        Assert.False(settings.HasSession(1));
    }

    [Fact]
    public async Task SaveLoadAndResetPreserveHudAndClassPreferences()
    {
        var store = new MemoryStore();
        var persistence = new PlayerPersistenceService(store);
        var preferences = new PlayerPreferences
        {
            ZClassId = "zombie_hunter", HClassId = "human_medic", AbilityHud = new(75, "bottom_right")
        };
        var snapshot = preferences.Snapshot();
        preferences.AbilityHud = AbilityHudPreferences.Default;
        await persistence.SaveAsync(5, snapshot);
        var loaded = await persistence.LoadAsync(5);
        Assert.NotNull(loaded);
        Assert.Equal(new(75, "bottom_right"), loaded.AbilityHud);
        Assert.Equal("zombie_hunter", loaded.ZClassId);
        Assert.Equal("human_medic", loaded.HClassId);
        Assert.False(loaded.AbilityHudScaleChangedInSession);
        Assert.False(loaded.AbilityHudPositionChangedInSession);
        await persistence.SaveAsync(5, preferences);
        Assert.Equal(AbilityHudPreferences.Default, (await persistence.LoadAsync(5))!.AbilityHud);
    }

    [Fact]
    public void UnknownStoredValuesNormalizeToSafeDefaults()
    {
        Assert.Equal(AbilityHudPreferences.Default, new AbilityHudPreferences(-1, "invalid").Normalize());
        Assert.Equal(new(75, "bottom_center"), new AbilityHudPreferences(75, "invalid").Normalize());
        Assert.Equal(new(100, "top_left"), new AbilityHudPreferences(999, "top_left").Normalize());
    }

    [Fact]
    public void EditingOnlyScaleStillLoadsTheSavedPosition()
    {
        var sessions = new PlayerSessionStore<PlayerPreferences>();
        var session = sessions.Create(1, new());
        var settings = new AbilityHudSettings(sessions);
        settings.Update(1, value => value with { ScalePercent = 75 });
        session.CompleteLoadMerged(data => data.MergeLoaded(new() { AbilityHud = new(50, "top_right") }));
        Assert.Equal(new(75, "top_right"), settings.Get(1));
    }

    [Fact]
    public void PresenterUpdatesOnlyTheOwnersAppearanceAndResetClearsSlotOverrides()
    {
        var sink = new Sink();
        var presenter = new AbilityHudPresenter(sink);
        var frame = new AbilityHudFrame([new("heal", "Heal", "heal", false, "Ready", "", "E")], true);
        presenter.Render(1, frame with { Appearance = new(50, "top_left") });
        Assert.Contains("1:AbilityBuffs:Scale50:True", sink.Calls);
        Assert.Contains("1:AbilityBuffs:Position_top_left:True", sink.Calls);
        sink.Calls.Clear();
        presenter.Render(2, frame);
        Assert.All(sink.Calls, value => Assert.StartsWith("2:", value));
        Assert.DoesNotContain(sink.Calls, value => value.Contains("Scale") || value.Contains("Position_"));
        sink.Calls.Clear();
        presenter.Render(1, frame with { Appearance = new(75, "bottom_right") });
        Assert.Equal(4, sink.Calls.Count);
        Assert.Contains("1:AbilityBuffs:Scale50:False", sink.Calls);
        Assert.Contains("1:AbilityBuffs:Scale75:True", sink.Calls);
        Assert.Contains("1:AbilityBuffs:Position_top_left:False", sink.Calls);
        Assert.Contains("1:AbilityBuffs:Position_bottom_right:True", sink.Calls);
        sink.Calls.Clear();
        presenter.Render(1, frame with { Appearance = new(75, "bottom_right") });
        Assert.Empty(sink.Calls);
        presenter.Clear(1);
        Assert.Contains("1:AbilityBuffs:Scale75:False", sink.Calls);
        Assert.Contains("1:AbilityBuffs:Position_bottom_right:False", sink.Calls);
        sink.Calls.Clear();
        presenter.Render(1, frame);
        Assert.DoesNotContain(sink.Calls, value => value.Contains("Scale") || value.Contains("Position_"));
    }

    [Fact]
    public void OnlySettingsMenuAllowsLivePreviewWithHideWhenMenuOpen()
    {
        var menu = DispatchProxy.Create<IMenuAPI, MenuStub>();
        Assert.False(AbilityHudSettings.ShouldHideForMenu(null, true));
        Assert.True(AbilityHudSettings.ShouldHideForMenu(menu, true));
        Assert.False(AbilityHudSettings.ShouldHideForMenu(menu, false));
        menu.Tag = AbilityHudSettings.PreviewMenuTag;
        Assert.False(AbilityHudSettings.ShouldHideForMenu(menu, true));
    }

    [Fact]
    public void MigrationMatchesModelAndAddsDefaultsWithoutReplacingClasses()
    {
        using var context = new ZombiePlagueDbContext(new DbContextOptionsBuilder<ZombiePlagueDbContext>()
            .UseNpgsql("Host=localhost;Database=hud_model_test;Username=test;Password=test").Options);
        Assert.False(context.Database.HasPendingModelChanges());
        var sql = context.GetService<IMigrator>().GenerateScript("20260813114955_CreatePlayers", "20260906160000_AddAbilityHudPreferences");
        Assert.Contains("ability_hud_scale", sql);
        Assert.Contains("DEFAULT 100", sql);
        Assert.Contains("ability_hud_position", sql);
        Assert.Contains("DEFAULT 'bottom_center'", sql);
        Assert.DoesNotContain("DROP TABLE", sql);
        Assert.DoesNotContain("DROP COLUMN", sql);
    }

    [Fact]
    public void PanoramaHasEverySupportedAppearanceClass()
    {
        var cssPath = Path.Combine(AppContext.BaseDirectory, "ability-hud", "content", "panorama", "styles", "custom_game",
            Path.GetFileName(CustomHudRuntime.CompiledStyle).Replace(".vcss_c", ".css"));
        var css = File.ReadAllText(cssPath);
        foreach (var scale in AbilityHudPreferences.Scales.Where(value => value != AbilityHudPreferences.DefaultScale))
            Assert.Contains($".AbilityBuffs.Scale{scale} {{ ui-scale: {scale}%; }}", css);
        foreach (var position in AbilityHudPreferences.Positions.Where(value => value != AbilityHudPreferences.DefaultPosition))
            Assert.Contains(".AbilityBuffs.Position_" + position + " {", css);
    }

    private sealed class Sink : IAbilityHudSink
    {
        public List<string> Calls { get; } = [];
        public void SetClass(int playerId, string panel, string name, bool enabled) => Calls.Add($"{playerId}:{panel}:{name}:{enabled}");
        public void SetText(int playerId, string panel, string value) => Calls.Add($"{playerId}:{panel}:{value}");
    }

    private sealed class MemoryStore : ISteamEntityStore<PlayerEntity>
    {
        private readonly Dictionary<ulong, PlayerEntity> _players = [];
        public Task<PlayerEntity?> FindAsync(ulong steamId, CancellationToken cancellationToken = default) => Task.FromResult(_players.GetValueOrDefault(steamId));
        public Task UpsertAsync(ulong steamId, Action<PlayerEntity> update, Action<PlayerEntity>? initialize = null, CancellationToken cancellationToken = default)
        {
            if (!_players.TryGetValue(steamId, out var entity))
            {
                entity = new() { SteamId = (long)steamId };
                initialize?.Invoke(entity);
                _players.Add(steamId, entity);
            }
            update(entity);
            return Task.CompletedTask;
        }
        public Task<bool> DeleteAsync(ulong steamId, CancellationToken cancellationToken = default) => Task.FromResult(_players.Remove(steamId));
    }

    /// <summary>Заглушка метки меню без запуска CS2</summary>
    public class MenuStub : DispatchProxy
    {
        private object? _tag;
        /// <inheritdoc />
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod!.Name == "get_Tag") return _tag;
            if (targetMethod.Name == "set_Tag") { _tag = args![0]; return null; }
            throw new NotSupportedException(targetMethod.Name);
        }
    }
}
