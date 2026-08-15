using Microsoft.EntityFrameworkCore;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Database;
using ZombiePlague.Core.Database.Entities;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Data.Service;

internal sealed class PlayerPersistenceService(IDbContextFactory<ZombiePlagueDbContext> dbContextFactory) : IPlayerPersistenceService
{
    public void InitializeDatabase()
    {
        using var context = dbContextFactory.CreateDbContext();
        context.Database.Migrate();
    }

    public async Task<PlayerPreferences?> LoadAsync(ulong steamId)
    {
        var databaseSteamId = checked((long)steamId);

        await using var context = await dbContextFactory
            .CreateDbContextAsync()
            .ConfigureAwait(false);

        return await context.Players
            .AsNoTracking()
            .Where(player => player.SteamId == databaseSteamId)
            .Select(player => new PlayerPreferences
            {
                ZClassId = player.ZombieClassId,
                HClassId = player.HumanClassId
            })
            .SingleOrDefaultAsync()
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(ulong steamId, PlayerPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        var databaseSteamId = checked((long)steamId);

        await using var context = await dbContextFactory
            .CreateDbContextAsync()
            .ConfigureAwait(false);

        var entity = await context.Players
            .SingleOrDefaultAsync(player => player.SteamId == databaseSteamId)
            .ConfigureAwait(false);

        if (entity is null)
        {
            context.Players.Add(new PlayerEntity
            {
                SteamId = databaseSteamId,
                ZombieClassId = preferences.ZClassId,
                HumanClassId = preferences.HClassId,
                UpdatedAtUtc = DateTime.UtcNow
            });
        }
        else
        {
            entity.ZombieClassId = preferences.ZClassId;
            entity.HumanClassId = preferences.HClassId;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await context.SaveChangesAsync().ConfigureAwait(false);
    }
}
