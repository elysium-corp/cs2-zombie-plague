using Microsoft.EntityFrameworkCore;
using Shop.Core.Data;

namespace Shop.Core.Database;

internal sealed class ShopSnapshotRepository(IDbContextFactory<ShopDbContext> contextFactory)
{
    public async Task<ShopSnapshot> LoadAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);
        var storefronts = await context.Storefronts
            .AsNoTracking()
            .OrderBy(item => item.ShopType)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var categories = await context.Categories
            .AsNoTracking()
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);
        var offers = await context.Offers
            .AsNoTracking()
            .Include(item => item.Privileges)
            .OrderBy(item => item.SortOrder)
            .ThenBy(item => item.Id)
            .AsSplitQuery()
            .ToArrayAsync(cancellationToken)
            .ConfigureAwait(false);

        return ShopSnapshotMapper.FromDatabase(storefronts, categories, offers);
    }
}
