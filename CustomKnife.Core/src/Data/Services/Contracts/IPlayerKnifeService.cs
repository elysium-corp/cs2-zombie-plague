namespace CustomKnife.Data.Services.Contracts;

internal interface IPlayerKnifeService
{
    Task InitializeAsync(ulong steamId, CancellationToken cancellationToken = default);

    string? GetKnifeId(ulong steamId);

    Task SetKnifeIdAsync(ulong steamId, string knifeId, CancellationToken cancellationToken = default);

    Task RemoveAsync(ulong steamId, CancellationToken cancellationToken = default);
}