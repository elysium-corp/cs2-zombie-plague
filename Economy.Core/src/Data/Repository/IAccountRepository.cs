using Economy.Core.Database.Entities;

namespace Economy.Core.Data.Repository;

internal interface IAccountRepository
{
    public Account? FindBySteamId(long steamId);

    public Account Create(long steamId, int initialBalance);

    public bool UpdateBalance(long steamId, int balance);

    public bool DeleteBySteamId(long steamId);
}