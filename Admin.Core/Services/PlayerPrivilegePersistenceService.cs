using Admin.Core.Data;
using Admin.Core.Database;
using Admin.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Services;

internal sealed class PlayerPrivilegePersistenceService(IDbContextFactory<AdminDbContext> dbContextFactory) : IPlayerPrivilegePersistenceService
{
    public async Task<PlayerPrivilege?> ExtendAsync(
        ulong steamId,
        string privilegeKey,
        TimeSpan duration,
        CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);
        var now = DateTime.UtcNow;

        if (!PrivilegeKey.TryParse(privilegeKey, out var key))
        {
            return null;
        }

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var entity = await context.PlayerPrivileges
            .Include(x => x.Privilege)
            .SingleOrDefaultAsync(
                x =>
                    x.SteamId == databaseSteamId &&
                    x.Privilege.Group == key.Group &&
                    x.Privilege.Code == key.Code,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (entity == null || entity.ExpiresAtUtc == null)
        {
            return null;
        }

        var startsAtUtc = entity.ExpiresAtUtc > now
            ? entity.ExpiresAtUtc.Value
            : now;

        entity.ExpiresAtUtc = startsAtUtc.Add(duration);
        entity.UpdatedAtUtc = now;

        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PlayerPrivilege(
            $"{entity.Privilege.Group}.{entity.Privilege.Code}",
            entity.ExpiresAtUtc,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc
        );
    }
    
    public async Task<PlayerPrivilege?> FindAsync(
        ulong steamId,
        string privilegeKey,
        CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);

        if (!PrivilegeKey.TryParse(privilegeKey, out var key))
        {
            return null;
        }

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await context.PlayerPrivileges
            .AsNoTracking()
            .Where(x =>
                x.SteamId == databaseSteamId &&
                x.Privilege.Group == key.Group &&
                x.Privilege.Code == key.Code
            )
            .Select(x => new PlayerPrivilege(
                x.Privilege.Group + "." + x.Privilege.Code,
                x.ExpiresAtUtc,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            ))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
    
    public async Task<IReadOnlyCollection<PlayerPrivilege>> LoadAsync(
        ulong steamId,
        CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);
        var now = DateTime.UtcNow;

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await context.PlayerPrivileges
            .AsNoTracking()
            .Where(x =>
                x.SteamId == databaseSteamId &&
                (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now)
            )
            .Select(x => new PlayerPrivilege(
                x.Privilege.Group + "." + x.Privilege.Code,
                x.ExpiresAtUtc,
                x.CreatedAtUtc,
                x.UpdatedAtUtc
            ))
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

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var privilegeId = await FindPrivilegeIdAsync(
            context,
            privilegeKey,
            cancellationToken
        ).ConfigureAwait(false);

        if (privilegeId == null)
        {
            throw new InvalidOperationException(
                $"Privilege '{privilegeKey}' does not exist in the database!"
            );
        }

        var entity = await context.PlayerPrivileges
            .SingleOrDefaultAsync(
                x =>
                    x.SteamId == databaseSteamId &&
                    x.PrivilegeId == privilegeId.Value,
                cancellationToken
            )
            .ConfigureAwait(false);

        if (entity == null)
        {
            entity = new PlayerPrivilegeEntity
            {
                SteamId = databaseSteamId,
                PrivilegeId = privilegeId.Value,
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

        await context.SaveChangesAsync(cancellationToken)
            .ConfigureAwait(false);

        return new PlayerPrivilege(
            privilegeKey,
            entity.ExpiresAtUtc,
            entity.CreatedAtUtc,
            entity.UpdatedAtUtc
        );
    }
    
    public async Task<bool> DeleteAsync(
        ulong steamId,
        string privilegeKey,
        CancellationToken cancellationToken = default)
    {
        var databaseSteamId = checked((long)steamId);

        var keyParsed = PrivilegeKey.TryParse(privilegeKey, out var key);

        if (!keyParsed)
        {
            return false;
        }

        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var deletedRows = await context.PlayerPrivileges
            .Where(x =>
                x.SteamId == databaseSteamId &&
                x.Privilege.Group == key.Group &&
                x.Privilege.Code == key.Code
            )
            .ExecuteDeleteAsync(cancellationToken)
            .ConfigureAwait(false);

        return deletedRows > 0;
    }
    
    private static async Task<int?> FindPrivilegeIdAsync(
        AdminDbContext context,
        string privilegeKey,
        CancellationToken cancellationToken)
    {
        if (!PrivilegeKey.TryParse(privilegeKey, out var key))
        {
            return null;
        }

        return await context.Privileges
            .Where(x =>
                x.Group == key.Group &&
                x.Code == key.Code
            )
            .Select(x => (int?)x.Id)
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}