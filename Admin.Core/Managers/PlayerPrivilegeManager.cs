using System.Collections.Concurrent;
using Admin.Core.Data;
using Admin.Core.Registry;
using Admin.Core.Services;
using Admin.Core.Store;
using Common.Database.Tasks;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Managers;

internal sealed class PlayerPrivilegeManager(
    IPlayerPrivilegeStore playerPrivilegeStore,
    IPlayerPrivilegePersistenceService persistenceService,
    DatabaseTaskTracker databaseTasks,
    SteamIdOperationQueue databaseOperations) : IPlayerPrivilegeManager
{
    // Для каждого подключения игрока создаётся уникальный идентификатор сессии.
    //
    // Он защищает runtime-хранилище от устаревших результатов асинхронных запросов:
    // если игрок вышел и успел подключиться снова, результат запроса от старого
    // подключения не должен перезаписать состояние новой сессии.
    private readonly ConcurrentDictionary<ulong, long> _sessions = new();

    // Используется для выдачи монотонно возрастающих идентификаторов игровых сессий.
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
    
    public Task ReloadAllAsync()
    {
        var tasks = _sessions.Keys
            .Select(ReloadAsync);

        return Task.WhenAll(tasks);
    }

    private async Task LoadAsync(ulong steamId, long sessionId)
    {
        var privileges = await databaseOperations
            .RunAsync(steamId, () => persistenceService.LoadAsync(steamId))
            .ConfigureAwait(false);

        ApplyLoaded(steamId, sessionId, privileges);
    }
    
    public Task<bool> ReloadAsync(ulong steamId)
    {
        if (!_sessions.TryGetValue(steamId, out var sessionId))
        {
            return Task.FromResult(false);
        }

        return databaseTasks.RunAsync(
            () => ReloadInternalAsync(steamId, sessionId),
            $"Reload admin privileges {steamId}"
        );
    }
    
    private async Task<bool> ReloadInternalAsync(ulong steamId, long sessionId)
    {
        var privileges = await databaseOperations
            .RunAsync(
                steamId,
                () => persistenceService.LoadAsync(steamId)
            )
            .ConfigureAwait(false);

        if (!_sessions.TryGetValue(steamId, out var currentSessionId) || currentSessionId != sessionId)
        {
            return false;
        }

        playerPrivilegeStore.Set(steamId, privileges);

        return true;
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