using Admin.Api.Data;
using Admin.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Services;

internal sealed class PrivilegePersistenceService(IDbContextFactory<AdminDbContext> dbContextFactory) : IPrivilegePersistenceService
{
    public async Task<IReadOnlyCollection<PrivilegeDefinition>> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var privileges = await context.Privileges
            .AsNoTracking()
            .Include(x => x.PrivilegePermissions)
            .ThenInclude(x => x.Permission)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return privileges
            .Select(x => new PrivilegeDefinition(
                Id: x.Code,
                Group: x.Group,
                Permissions: x.PrivilegePermissions
                    .Select(link => link.Permission.Key)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase)
            ))
            .ToArray();
    }
}