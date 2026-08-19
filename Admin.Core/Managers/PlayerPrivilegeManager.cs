using System.Collections.Concurrent;
using Admin.Core.Data;
using Admin.Core.Services;
using Admin.Core.Store;
using Common.Database.Tasks;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Managers;

internal sealed class PlayerPrivilegeManager(
    ISwiftlyCore core,
    IPlayerPrivilegeStore playerPrivilegeStore,
    IPlayerPrivilegePersistenceService persistenceService,
    DatabaseTaskTracker databaseTasks)
{
    private readonly ConcurrentDictionary<ulong, long> _sessions = new();

    private long _nextSessionId;

    public void Initialize(IPlayer player)
    {
        if (!CanInitialize(player))
        {
            return;
        }

        var steamId = player.SteamID;
        var sessionId = Interlocked.Increment(ref _nextSessionId);

        _sessions[steamId] = sessionId;

        playerPrivilegeStore.Remove(steamId);

        databaseTasks.Run(() => LoadAsync(steamId, sessionId), $"Load admin privileges {steamId}");
    }

    public void Remove(IPlayer player)
    {
        var steamId = player.SteamID;

        _sessions.TryRemove(steamId, out _);
        playerPrivilegeStore.Remove(steamId);
    }

    public void StopAndWait()
    {
        _sessions.Clear();

        databaseTasks.StopAndWait();
    }

    private async Task LoadAsync(ulong steamId, long sessionId)
    {
        var privileges = await persistenceService.LoadAsync(steamId).ConfigureAwait(false);

        core.Scheduler.NextWorldUpdate(() => ApplyLoaded(steamId, sessionId, privileges));
    }

    private void ApplyLoaded(ulong steamId, long sessionId, IReadOnlyCollection<PlayerPrivilege> privileges)
    {
        if (!_sessions.TryGetValue(steamId, out var currentSessionId) || currentSessionId != sessionId)
        {
            return;
        }

        playerPrivilegeStore.Set(steamId, privileges);
    }

    private static bool CanInitialize(IPlayer player)
    {
        return player is { IsValid: true, IsAuthorized: true, IsFakeClient: false } && player.SteamID != 0;
    }
}