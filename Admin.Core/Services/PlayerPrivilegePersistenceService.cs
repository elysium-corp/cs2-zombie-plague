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
}