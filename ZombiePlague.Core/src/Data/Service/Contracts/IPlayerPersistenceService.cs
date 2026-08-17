using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Data.Service.Contracts;

internal interface IPlayerPersistenceService
{
    Task<PlayerPreferences?> LoadAsync(ulong steamId);

    Task SaveAsync(ulong steamId, PlayerPreferences preferences);
}
