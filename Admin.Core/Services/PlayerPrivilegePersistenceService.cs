using Admin.Core.Data;
using Admin.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Services;

internal sealed class PlayerPrivilegePersistenceService(IDbContextFactory<AdminDbContext> dbContextFactory) : IPlayerPrivilegePersistenceService
{
    public async Task<IReadOnlyCollection<PlayerPrivilege>> LoadAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);
        var now = DateTime.UtcNow;

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.PlayerPrivileges
            .AsNoTracking()
            .Where(x => x.SteamId == databaseSteamId && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now))
            .Select(x => new PlayerPrivilege(x.PrivilegeKey, x.ExpiresAtUtc))
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    
    public async Task<PlayerPrivilege> UpsertAsync(
        ulong steamId,
        string privilegeKey,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);
        var now = DateTime.UtcNow;

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var entity = await context.PlayerPrivileges
            .SingleOrDefaultAsync(
                x => x.SteamId == databaseSteamId && x.PrivilegeKey == privilegeKey,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (entity == null)
        {
            entity = new()
            {
                SteamId = databaseSteamId,
                PrivilegeKey = privilegeKey,
                ExpiresAtUtc = expiresAtUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            context.PlayerPrivileges.Add(entity);
        }
        else
        {
            entity.ExpiresAtUtc = expiresAtUtc;
            entity.UpdatedAtUtc = now;
        }

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return new PlayerPrivilege(entity.PrivilegeKey, entity.ExpiresAtUtc);
    }
    
    public async Task<bool> DeleteAsync(ulong steamId, string privilegeKey, CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var deletedRows = await context.PlayerPrivileges
            .Where(x => x.SteamId == databaseSteamId && x.PrivilegeKey == privilegeKey)
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return deletedRows > 0;
    }
}