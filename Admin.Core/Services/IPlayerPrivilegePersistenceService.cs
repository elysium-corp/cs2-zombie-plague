using Admin.Core.Data;

namespace Admin.Core.Services;

internal interface IPlayerPrivilegePersistenceService
{
    Task<PlayerPrivilege?> ExtendAsync(
        ulong steamId,
        string privilegeKey,
        TimeSpan duration,
        CancellationToken cancellationToken = default);
    
    Task<PlayerPrivilege?> FindAsync(
        ulong steamId,
        string privilegeKey,
        CancellationToken cancellationToken = default);
    
    Task<IReadOnlyCollection<PlayerPrivilege>> LoadAsync(ulong steamId, CancellationToken cancellationToken = default);

    Task<PlayerPrivilege> UpsertAsync(
        ulong steamId,
        string privilegeKey,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(ulong steamId, string privilegeKey, CancellationToken cancellationToken = default);
}