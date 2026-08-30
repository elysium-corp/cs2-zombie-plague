using Economy.Core.Data.Rules;
using Economy.Core.Database;
using Microsoft.EntityFrameworkCore;

namespace Economy.Core.Services;

internal sealed class EconomyRulesRepository(
    IDbContextFactory<EconomyDbContext> contextFactory
)
{
    public async Task<EconomyRulesSnapshot?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        var settings = await context.Settings
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == 1, cancellationToken)
            .ConfigureAwait(false);

        if (settings is null)
        {
            return null;
        }

        var roleRules = await context.RoleRules
            .AsNoTracking()
            .Where(rule => rule.Enabled)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        var weaponRules = await context.WeaponRules
            .AsNoTracking()
            .Where(rule => rule.Enabled)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return EconomyRulesSnapshot.FromDatabase(settings, roleRules, weaponRules);
    }
}
