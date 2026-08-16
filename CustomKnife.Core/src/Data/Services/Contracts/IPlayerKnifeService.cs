namespace CustomKnife.Data.Services.Contracts;

internal interface IPlayerKnifeService
{
    void Initialize(ulong steamId);

    string? GetKnifeId(ulong steamId);

    Task SetKnifeIdAsync(ulong steamId, string knifeId, CancellationToken cancellationToken = default);

    void Remove(ulong steamId);
}