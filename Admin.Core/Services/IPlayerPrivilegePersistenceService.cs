using Admin.Core.Data;

namespace Admin.Core.Services;

internal interface IPlayerPrivilegePersistenceService
{
    Task<IReadOnlyCollection<PlayerPrivilege>> LoadAsync(ulong steamId, CancellationToken cancellationToken = default);

    Task<PlayerPrivilege> UpsertAsync(
        ulong steamId,
        string privilegeKey,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(ulong steamId, string privilegeKey, CancellationToken cancellationToken = default);
}