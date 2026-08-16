using CustomKnife.Data.Services.Contracts;
using CustomKnife.Database;
using CustomKnife.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace CustomKnife.Data.Services;

internal sealed class PlayerKnifePersistenceService(IDbContextFactory<CustomKnifeDbContext> dbContextFactory) : IPlayerKnifePersistenceService
{
    public async Task<string?> LoadAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await context.PlayerKnives
            .AsNoTracking()
            .Where(playerKnife => playerKnife.SteamId == databaseSteamId)
            .Select(playerKnife => playerKnife.KnifeId)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task SaveAsync(ulong steamId, string knifeId, CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.PlayerKnives
            .SingleOrDefaultAsync(
                playerKnife => playerKnife.SteamId == databaseSteamId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new PlayerKnifeEntity
            {
                SteamId = databaseSteamId,
                KnifeId = knifeId,
                UpdatedAtUtc = DateTime.UtcNow
            };

            context.PlayerKnives.Add(entity);
        }
        else
        {
            entity.KnifeId = knifeId;
            entity.UpdatedAtUtc = DateTime.UtcNow;
        }

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}