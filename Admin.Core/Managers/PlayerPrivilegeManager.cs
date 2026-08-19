using System.Collections.Concurrent;
using Admin.Core.Data;
using Admin.Core.Registry;
using Admin.Core.Services;
using Admin.Core.Store;
using Common.Database.Tasks;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Managers;

internal sealed class PlayerPrivilegeManager(
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
    
    public Task<bool> ExtendAsync(ulong steamId, string privilegeKey, TimeSpan duration)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privilegeKey);

        if (steamId == 0 || duration <= TimeSpan.Zero)
        {
            return Task.FromResult(false);
        }

        var privilege = privilegeRegistry.Find(privilegeKey);

        if (privilege == null)
        {
            return Task.FromResult(false);
        }

        return databaseTasks.RunAsync(
            () => ExtendInternalAsync(steamId, privilege.Key, duration),
            $"Extend privilege {privilege.Key} for {steamId}"
        );
    }
    
    private async Task<bool> ExtendInternalAsync(ulong steamId, string privilegeKey, TimeSpan duration)
    {
        var playerPrivilege = await databaseOperations
            .RunAsync(
                steamId,
                () => persistenceService.ExtendAsync(steamId, privilegeKey, duration)
            )
            .ConfigureAwait(false);

        if (playerPrivilege == null)
        {
            return false;
        }

        ApplyGranted(steamId, playerPrivilege);

        return true;
    }
    
    public Task<PlayerPrivilege?> FindAsync(ulong steamId, string privilegeKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privilegeKey);

        if (steamId == 0)
        {
            return Task.FromResult<PlayerPrivilege?>(null);
        }

        var privilege = privilegeRegistry.Find(privilegeKey);
        var canonicalKey = privilege?.Key ?? privilegeKey;

        return databaseTasks.RunAsync(
            () => databaseOperations.RunAsync(
                steamId,
                () => persistenceService.FindAsync(steamId, canonicalKey)
            ),
            $"Find privilege {canonicalKey} for {steamId}"
        );
    }

    private async Task LoadAsync(ulong steamId, long sessionId)
    {
        var privileges = await databaseOperations
            .RunAsync(steamId, () => persistenceService.LoadAsync(steamId))
            .ConfigureAwait(false);

        ApplyLoaded(steamId, sessionId, privileges);
    }
    
    public Task<bool> GrantAsync(ulong steamId, string privilegeKey, DateTime? expiresAtUtc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privilegeKey);

        if (steamId == 0)
        {
            return Task.FromResult(false);
        }

        var privilege = privilegeRegistry.Find(privilegeKey);

        if (privilege == null)
        {
            return Task.FromResult(false);
        }

        if (expiresAtUtc is { Kind: not DateTimeKind.Utc })
        {
            throw new ArgumentException("Privilege expiration date must be UTC!", nameof(expiresAtUtc));
        }

        if (expiresAtUtc is { } expiresAt && expiresAt <= DateTime.UtcNow)
        {
            return Task.FromResult(false);
        }

        return databaseTasks.RunAsync(
            () => GrantInternalAsync(steamId, privilege.Key, expiresAtUtc),
            $"Grant privilege {privilege.Key} to {steamId}"
        );
    }
    
    private async Task<bool> GrantInternalAsync(ulong steamId, string privilegeKey, DateTime? expiresAtUtc)
    {
        var playerPrivilege = await databaseOperations
            .RunAsync(steamId, () => persistenceService.UpsertAsync(steamId, privilegeKey, expiresAtUtc))
            .ConfigureAwait(false);

        ApplyGranted(steamId, playerPrivilege);

        return true;
    }
    
    public Task<bool> RevokeAsync(ulong steamId, string privilegeKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(privilegeKey);

        if (steamId == 0)
        {
            return Task.FromResult(false);
        }

        var privilege = privilegeRegistry.Find(privilegeKey);
        var canonicalKey = privilege?.Key ?? privilegeKey;

        return databaseTasks.RunAsync(
            () => RevokeInternalAsync(steamId, canonicalKey),
            $"Revoke privilege {canonicalKey} from {steamId}"
        );
    }
    
    private async Task<bool> RevokeInternalAsync(ulong steamId, string privilegeKey)
    {
        var removed = await databaseOperations
            .RunAsync(steamId, () => persistenceService.DeleteAsync(steamId, privilegeKey))
            .ConfigureAwait(false);

        if (!removed)
        {
            return false;
        }

        ApplyRevoked(steamId, privilegeKey);

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