using Admin.Core.Data;
using Admin.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Services;

internal sealed class PrivilegePersistenceService(IDbContextFactory<AdminDbContext> dbContextFactory) : IPrivilegePersistenceService
{
    public async Task<IReadOnlyCollection<PrivilegeDefinition>> LoadAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await dbContextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var privileges = await context.Privileges
            .AsNoTracking()
            .Select(x => new
            {
                x.Group,
                x.Code,
                x.DisplayName,
                x.ChatColor,
                x.HudColor,
                x.ColorPriority,

                Permissions = x.PrivilegePermissions
                    .Select(link => link.Permission.Key)
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return privileges
            .Select(x => new PrivilegeDefinition(
                Id: x.Code,
                Group: x.Group,
                Permissions: x.Permissions.ToHashSet(StringComparer.OrdinalIgnoreCase),
                DisplayName: x.DisplayName,
                ChatColor: x.ChatColor,
                HudColor: x.HudColor,
                ColorPriority: x.ColorPriority
            ))
            .ToArray();
    }
}
