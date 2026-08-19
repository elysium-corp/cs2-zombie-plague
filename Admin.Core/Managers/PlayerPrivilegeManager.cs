using System.Collections.Concurrent;
using Admin.Core.Data;
using Admin.Core.Registry;
using Admin.Core.Services;
using Admin.Core.Store;
using Common.Database.Tasks;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Managers;

internal sealed class PlayerPrivilegeManager(
    ISwiftlyCore core,
    IPrivilegeRegistry privilegeRegistry,
    IPlayerPrivilegeStore playerPrivilegeStore,
    IPlayerPrivilegePersistenceService persistenceService,
    DatabaseTaskTracker databaseTasks,
    SteamIdOperationQueue databaseOperations)
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
        var privileges = await databaseOperations
            .RunAsync(steamId, () => persistenceService.LoadAsync(steamId))
            .ConfigureAwait(false);

        core.Scheduler.NextWorldUpdate(() => ApplyLoaded(steamId, sessionId, privileges));
    }
    
    public bool Grant(ulong steamId, string privilegeKey, DateTime? expiresAtUtc = null)
    {
        var privilege = privilegeRegistry.Find(privilegeKey);

        if (privilege == null)
        {
            return false;
        }

        if (expiresAtUtc is { } expiresAt && expiresAt <= DateTime.UtcNow)
        {
            return false;
        }

        var canonicalKey = $"{privilege.Group}.{privilege.Id}";

        databaseTasks.Run(
            () => GrantAsync(steamId, canonicalKey, expiresAtUtc),
            $"Grant privilege {canonicalKey} to {steamId}"
        );

        return true;
    }
    
    private async Task GrantAsync(ulong steamId, string privilegeKey, DateTime? expiresAtUtc)
    {
        var playerPrivilege = await databaseOperations
            .RunAsync(steamId, () => persistenceService.UpsertAsync(steamId, privilegeKey, expiresAtUtc))
            .ConfigureAwait(false);

        core.Scheduler.NextWorldUpdate(() => ApplyGranted(steamId, playerPrivilege));
    }
    
    public void Revoke(ulong steamId, string privilegeKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privilegeKey);

        var privilege = privilegeRegistry.Find(privilegeKey);
        var canonicalKey = privilege == null ? privilegeKey : $"{privilege.Group}.{privilege.Id}";

        databaseTasks.Run(
            () => RevokeAsync(steamId, canonicalKey),
            $"Revoke privilege {canonicalKey} from {steamId}"
        );
    }
    
    private async Task RevokeAsync(ulong steamId, string privilegeKey)
    {
        var removed = await databaseOperations
            .RunAsync(steamId, () => persistenceService.DeleteAsync(steamId, privilegeKey))
            .ConfigureAwait(false);

        if (!removed)
        {
            return;
        }

        core.Scheduler.NextWorldUpdate(() => ApplyRevoked(steamId, privilegeKey));
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
    
    private void ApplyGranted(ulong steamId, PlayerPrivilege privilege)
    {
        if (!_sessions.ContainsKey(steamId))
        {
            return;
        }

        var privileges = playerPrivilegeStore
            .Get(steamId)
            .Values
            .Where(x => !string.Equals(x.Key, privilege.Key, StringComparison.OrdinalIgnoreCase))
            .Append(privilege);

        playerPrivilegeStore.Set(steamId, privileges);
    }
    
    private void ApplyRevoked(ulong steamId, string privilegeKey)
    {
        if (!_sessions.ContainsKey(steamId))
        {
            return;
        }

        var privileges = playerPrivilegeStore
            .Get(steamId)
            .Values
            .Where(x => !string.Equals(x.Key, privilegeKey, StringComparison.OrdinalIgnoreCase));

        playerPrivilegeStore.Set(steamId, privileges);
    }
}