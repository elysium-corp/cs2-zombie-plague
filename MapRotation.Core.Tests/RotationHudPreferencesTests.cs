using System.Collections.Concurrent;
using System.Reflection;
using Common.Database.Tasks;
using CustomHud.Api;
using MapRotation.Core.Database;
using Microsoft.Extensions.Logging.Abstractions;
using SwiftlyS2.Shared.Players;
using Xunit;

namespace MapRotation.Core.Tests;

public sealed class RotationHudPreferencesTests
{
    [Fact]
    public async Task LateLoadDoesNotOverwriteChoiceAndRepeatedChangesSaveInOrder()
    {
        var store = new Store { Load = new(TaskCreationOptions.RunContinuationsAsynchronously) };
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var preferences = new RotationHudPreferences(store, queue, tracker);
        var player = Player(10, 100);
        Assert.Equal(HudMenuOrientation.Horizontal, preferences.Get(player).Orientation);
        var choice = new HudMenuPresentation { Orientation = HudMenuOrientation.Vertical, ScalePercent = 120 };
        Assert.True(preferences.Set(player, choice));
        store.Load.SetResult(new() { Orientation = HudMenuOrientation.Horizontal, ScalePercent = 80 });
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(choice, preferences.Get(player)); Assert.Equal(choice, store.Saved[100]);
        preferences.Set(player, choice with { ScalePercent = 80 });
        preferences.Set(player, choice with { ScalePercent = 100 });
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(100, store.Saved[100].ScalePercent);
    }

    [Fact]
    public async Task LateResponseForReusedSlotCannotChangeReplacementPlayer()
    {
        var gate = new TaskCompletionSource<HudMenuPresentation?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var store = new Store { Load = gate };
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var preferences = new RotationHudPreferences(store, queue, tracker);
        preferences.Get(Player(10, 100));
        await store.LoadStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
        store.Load = null;
        var replacement = Player(11, 200);
        var choice = new HudMenuPresentation { Orientation = HudMenuOrientation.Vertical, ScalePercent = 80 };
        preferences.Set(replacement, choice);
        gate.SetResult(new() { ScalePercent = 120 });
        await queue.RunAsync(100, () => Task.CompletedTask);
        await queue.RunAsync(200, () => Task.CompletedTask);
        Assert.Equal(choice, preferences.Get(replacement));
        Assert.False(store.Saved.ContainsKey(100));
        Assert.Equal(choice, store.Saved[200]);
    }

    [Fact]
    public async Task ReconnectReadsAfterPreviousSessionsPendingSave()
    {
        var store = new Store();
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var preferences = new RotationHudPreferences(store, queue, tracker);
        var choice = new HudMenuPresentation { Orientation = HudMenuOrientation.Vertical, ScalePercent = 80 };
        var player = Player(10, 100);
        preferences.Set(player, choice);
        preferences.Forget(player.PlayerID);
        var reconnected = Player(11, 100);
        preferences.Get(reconnected);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(choice, preferences.Get(reconnected));
    }

    [Fact]
    public async Task FailedReadNeverSavesDefaultsAndFailedWriteCanRetry()
    {
        var store = new Store { FailReads = true, FailWrites = true };
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var preferences = new RotationHudPreferences(store, queue, tracker);
        var player = Player(10, 100);
        preferences.Get(player); preferences.Forget(player.PlayerID);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Empty(store.Saved);
        player = Player(11, 100);
        var choice = new HudMenuPresentation { Orientation = HudMenuOrientation.Vertical, ScalePercent = 120 };
        preferences.Set(player, choice);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Empty(store.Saved);
        Assert.Equal(choice, preferences.Get(player));
        store.FailWrites = false;
        preferences.Set(player, choice);
        await queue.RunAsync(100, () => Task.CompletedTask);
        Assert.Equal(choice, store.Saved[100]);
    }

    [Fact]
    public void InvalidPresentationIsRejectedBeforeCreatingStorageWork()
    {
        var store = new Store();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        using var preferences = new RotationHudPreferences(store, new(), tracker);
        Assert.False(preferences.Set(Player(1, 100), new() { ScalePercent = 125 }));
        Assert.False(preferences.Set(Player(1, 100), new() { Orientation = (HudMenuOrientation)9 }));
        Assert.False(store.LoadStarted.Task.IsCompleted);
    }

    private static IPlayer Player(ulong session, ulong steamId)
    {
        var player = DispatchProxy.Create<IPlayer, PlayerStub>();
        ((PlayerStub)(object)player).Handler = method => method.Name switch
        {
            "get_PlayerID" => 3, "get_SessionId" => session, "get_SteamID" => steamId,
            "get_IsValid" => true, "get_IsFakeClient" => false,
            _ => throw new InvalidOperationException(method.Name)
        };
        return player;
    }

    public class PlayerStub : DispatchProxy
    {
        public Func<MethodInfo, object?> Handler = null!;
        protected override object? Invoke(MethodInfo? method, object?[]? args) => Handler(method!);
    }

    private sealed class Store : IRotationHudPreferenceStore
    {
        public TaskCompletionSource<HudMenuPresentation?>? Load { get; set; }
        public TaskCompletionSource LoadStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public ConcurrentDictionary<ulong, HudMenuPresentation> Saved { get; } = new();
        public bool FailReads { get; set; }
        public bool FailWrites { get; set; }
        public Task<HudMenuPresentation?> LoadAsync(ulong steamId, CancellationToken token)
        {
            var result = FailReads ? Task.FromException<HudMenuPresentation?>(new IOException("Read failed"))
                : Load?.Task.WaitAsync(token) ?? Task.FromResult(Saved.GetValueOrDefault(steamId));
            LoadStarted.TrySetResult();
            return result;
        }
        public Task SaveAsync(ulong steamId, HudMenuPresentation presentation, CancellationToken token)
        {
            if (FailWrites) throw new IOException("Write failed");
            Saved[steamId] = presentation;
            return Task.CompletedTask;
        }
    }
}
