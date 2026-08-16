using Common.Database.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Common.Database.Storage;

internal sealed class EfSteamEntityStore<TContext, TEntity>(IDbContextFactory<TContext> contextFactory) : ISteamEntityStore<TEntity>
    where TContext : DbContext
    where TEntity : class, ISteamEntity, new()
{
    public async Task<TEntity?> FindAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var dbSteamId = checked((long)steamId);

        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await context
            .Set<TEntity>()
            .AsNoTracking()
            .SingleOrDefaultAsync(
                entity => EF.Property<long>(entity, nameof(ISteamEntity.SteamId)) == dbSteamId,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    public async Task UpsertAsync(
        ulong steamId,
        Action<TEntity> update,
        Action<TEntity>? initialize = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(update);

        var dbSteamId = checked((long)steamId);

        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var entities = context.Set<TEntity>();

        var entity = await entities
            .SingleOrDefaultAsync(
                entity => EF.Property<long>(entity, nameof(ISteamEntity.SteamId)) == dbSteamId,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (entity is null)
        {
            entity = new TEntity
            {
                SteamId = dbSteamId
            };

            initialize?.Invoke(entity);

            entities.Add(entity);
        }

        update(entity);

        await context
            .SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<bool> DeleteAsync(
        ulong steamId,
        CancellationToken cancellationToken = default)
    {
        var dbSteamId = checked((long)steamId);

        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var deletedRows = await context
            .Set<TEntity>()
            .Where(entity => EF.Property<long>(entity, nameof(ISteamEntity.SteamId)) == dbSteamId)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return deletedRows == 1;
    }
}