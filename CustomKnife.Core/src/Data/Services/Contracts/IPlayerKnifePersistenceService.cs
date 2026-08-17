namespace CustomKnife.Data.Services.Contracts;

internal interface IPlayerKnifePersistenceService
{
    Task<string?> LoadAsync(ulong steamId, CancellationToken cancellationToken = default);

    Task SaveAsync(ulong steamId, string knifeId, CancellationToken cancellationToken = default);
}