namespace Economy.Core.Services;

internal interface IAccountPersistenceService
{
    public int LoadOrCreateBalance(long steamId, int initialBalance);

    public void SaveBalance(long steamId, int balance);
}