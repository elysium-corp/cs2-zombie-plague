namespace Economy.Core.Services;

internal interface IAccountPersistenceService
{
    Task<int?> LoadAsync(ulong steamId, CancellationToken cancellationToken = default);

    Task SaveAsync(ulong steamId, int balance, CancellationToken cancellationToken = default);
}