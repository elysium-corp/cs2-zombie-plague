namespace CustomKnife.Data.Services.Contracts;

internal interface IPlayerKnifeService
{
    void Initialize(ulong steamId);

    string? GetKnifeId(ulong steamId);

    void SetKnifeId(ulong steamId, string knifeId);

    void Remove(ulong steamId);
}