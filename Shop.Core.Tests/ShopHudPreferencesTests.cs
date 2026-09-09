using System.Reflection;
using Common.Database.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;
using Shop.Core.Database;
using Shop.Core.Database.Entities;
using Shop.Core.Hud;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Tests;

public sealed class ShopHudPreferencesTests
{
    [Fact]
    public async Task UnsetScaleTracksServerDefaultAndExplicitChoiceRemainsPersonal()
    {
        var store = new StoreStub();
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var prefs = new ShopHudPreferences(store, queue, tracker);
        var player = Player(10, 100);
        Assert.Equal(115, prefs.Get(player, 115).ScalePercent);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(85, prefs.Get(player, 85).ScalePercent);
        Assert.Empty(store.Saved);
        prefs.Set(player, 75);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(75, prefs.Get(player, 125).ScalePercent);
    }

    [Fact]
    public async Task ChoiceBeforeLoadCompletesWinsAndPersistsInOrder()
    {
        var store = new StoreStub { Load = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var prefs = new ShopHudPreferences(store, queue, tracker);
        var player = Player(10, 100);
        Assert.Equal(ShopHudSaveStatus.Loading, prefs.Get(player).Status);
        Assert.True(prefs.Set(player, 115));
        Assert.Equal(115, prefs.Get(player).ScalePercent);
        store.Load.SetResult(85);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(115, prefs.Get(player).ScalePercent);
        Assert.Equal(115, store.Saved[100]);
        prefs.Set(player, 75);
        prefs.Set(player, 125);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(125, store.Saved[100]);
        Assert.Equal(ShopHudSaveStatus.Ready, prefs.Get(player).Status);
    }

    [Fact]
    public async Task ReusedSlotAndLateOldLoadCannotChangeNewPlayersSettings()
    {
        var gate = new TaskCompletionSource<int?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new StoreStub { Load = gate };
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var prefs = new ShopHudPreferences(store, queue, tracker);
        prefs.Get(Player(10, 100));
        store.Load = null;
        var replacement = Player(11, 200);
        prefs.Get(replacement);
        prefs.Set(replacement, 75);
        gate.SetResult(125);
        await queue.RunAsync(100, () => Task.CompletedTask);
        await queue.RunAsync(200, () => Task.CompletedTask);
        Assert.Equal(75, prefs.Get(replacement).ScalePercent);
        Assert.False(store.Saved.ContainsKey(100));
        Assert.Equal(75, store.Saved[200]);
    }

    [Fact]
    public async Task FailedWriteIsVisibleAndSameSelectionCanRetryThenReload()
    {
        var store = new StoreStub { FailWrites = true };
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var prefs = new ShopHudPreferences(store, queue, tracker);
        var player = Player(10, 100);
        prefs.Set(player, 85);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(ShopHudSaveStatus.Failed, prefs.Get(player).Status);
        Assert.Empty(store.Saved);
        store.FailWrites = false;
        prefs.Set(player, 85);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(ShopHudSaveStatus.Ready, prefs.Get(player).Status);
        prefs.Forget(player.PlayerID);
        var reconnected = Player(11, 100);
        prefs.Get(reconnected);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(85, prefs.Get(reconnected).ScalePercent);
    }

    [Fact]
    public async Task FailedLoadNeverOverwritesSavedScaleWithDefault()
    {
        var store = new StoreStub { FailReads = true };
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var prefs = new ShopHudPreferences(store, queue, tracker);
        var player = Player(10, 100);
        prefs.Get(player);
        prefs.Forget(player.PlayerID);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Empty(store.Saved);
        prefs.Set(Player(11, 100), 125);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(125, store.Saved[100]);
    }

    [Fact]
    public void PreferenceMigrationMatchesModelAndDoesNotModifyShopCatalog()
    {
        using var db = new ShopDbContext(new DbContextOptionsBuilder<ShopDbContext>()
            .UseNpgsql("Host=localhost;Database=metadata;Username=test;Password=test").Options);
        var sql = db.GetService<IMigrator>().GenerateScript("20260905060000_AddStandardWeaponsAndCategorizeOffers",
            "20260909020000_AddShopPlayerPreferences");
        Assert.Contains("CREATE TABLE shop.player_preferences", sql);
        Assert.Contains("steam_id BIGINT PRIMARY KEY", sql);
        Assert.Contains("hud_scale IN (" + string.Join(", ", ShopHudPreference.Scales) + ")", sql);
        Assert.DoesNotContain("shop.offers", sql);
        Assert.DoesNotContain("mark_fallback_dirty", sql);
        var entity = db.Model.FindEntityType(typeof(ShopPlayerPreferenceEntity))!;
        Assert.Equal("player_preferences", entity.GetTableName());
        Assert.Equal("shop", entity.GetSchema());
        Assert.Equal(100, entity.FindProperty(nameof(ShopPlayerPreferenceEntity.HudScale))!.GetDefaultValue());
    }

    private static IPlayer Player(ulong session, ulong steamId)
    {
        var player = DispatchProxy.Create<IPlayer, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)player).Handler = (method, _) => method.Name switch
        {
            "get_PlayerID" => 3, "get_SessionId" => session, "get_SteamID" => steamId,
            "get_IsValid" or "get_IsAuthorized" => true, "get_IsFakeClient" => false,
            _ => throw new InvalidOperationException(method.Name)
        };
        return player;
    }

    private sealed class StoreStub : IShopHudPreferenceStore
    {
        public TaskCompletionSource<int?>? Load { get; set; }
        public Dictionary<ulong, int> Saved { get; } = [];
        public bool FailWrites { get; set; }
        public bool FailReads { get; set; }
        public Task<int?> LoadAsync(ulong steamId, CancellationToken token) => FailReads
            ? Task.FromException<int?>(new IOException("Read failed")) : Load?.Task.WaitAsync(token)
                ?? Task.FromResult(Saved.TryGetValue(steamId, out var saved) ? (int?)saved : null);
        public Task SaveAsync(ulong steamId, int scalePercent, CancellationToken token)
        {
            if (FailWrites) throw new IOException("Write failed");
            Saved[steamId] = scalePercent;
            return Task.CompletedTask;
        }
    }
}
