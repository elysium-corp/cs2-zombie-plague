using Microsoft.EntityFrameworkCore;
using Economy.Core.Database;
using Economy.Core.Database.Entities;

namespace Economy.Core.Data.Repository;

internal sealed class AccountRepository(IDbContextFactory<EconomyDbContext> dbContextFactory) : IAccountRepository
{
    public Account? FindBySteamId(long steamId)
    {
        using var context = dbContextFactory.CreateDbContext();
        
        return context.Accounts
            .AsNoTracking()
            .SingleOrDefault(account => account.SteamId == steamId);
    }

    public Account Create(long steamId, int initialBalance)
    {
        using var context = dbContextFactory.CreateDbContext();

        var now = DateTime.UtcNow;

        var account = new Account
        {
            SteamId = steamId,
            Balance = initialBalance,
            CreatedAt = now,
            UpdatedAt = now
        };
        
        context.Accounts.Add(account);
        context.SaveChanges();
        
        return account;
    }

    public bool UpdateBalance(long steamId, int balance)
    {
        using var context = dbContextFactory.CreateDbContext();

        var updatedRows = context.Accounts
            .Where(account => account.SteamId == steamId)
            .ExecuteUpdate(setters => setters
                .SetProperty(account => account.Balance, balance)
                .SetProperty(account => account.UpdatedAt, DateTime.UtcNow)
            );

        return updatedRows == 1;
    }
    
    public bool DeleteBySteamId(long steamId)
    {
        using var context = dbContextFactory.CreateDbContext();

        var deletedRows = context.Accounts
            .Where(account => account.SteamId == steamId)
            .ExecuteDelete();

        return deletedRows == 1;
    }
}