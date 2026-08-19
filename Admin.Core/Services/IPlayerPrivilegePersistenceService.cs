using Admin.Core.Data;

namespace Admin.Core.Services;

internal interface IPlayerPrivilegePersistenceService
{
    Task<IReadOnlyCollection<PlayerPrivilege>> LoadAsync(ulong steamId, CancellationToken cancellationToken = default);
}