using Menu.Core.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace Menu.Core.Database.Repositories;

/// <summary>
/// Читает только готовые immutable release targets, никогда не собирая Draft в runtime.
/// </summary>
internal sealed class MenuReleaseRepository(IDbContextFactory<MenuDbContext> contextFactory)
{
    public async Task<MenuReleaseTargetSnapshot?> LoadActiveTargetAsync(
        string serverKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);

        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await context.ReleaseHeads
            .AsNoTracking()
            .Where(head => head.ServerKey == serverKey)
            .Select(head => new MenuReleaseTargetSnapshot(
                head.Target.ReleaseId,
                head.Target.Release.ReleaseNumber,
                head.Target.Release.SchemaVersion,
                head.Target.Release.MenuCoreApiVersion,
                head.Target.ServerKey,
                head.Target.ServerGroupKey,
                head.Target.ArtifactJson,
                head.Target.Checksum,
                head.Target.CapabilityManifestJson,
                head.Target.Release.PublishedAt))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MenuReleaseTargetSnapshot?> LoadTargetAsync(
        long releaseId,
        string serverKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(releaseId);
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);

        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await context.ReleaseTargets
            .AsNoTracking()
            .Where(target => target.ReleaseId == releaseId && target.ServerKey == serverKey)
            .Select(target => new MenuReleaseTargetSnapshot(
                target.ReleaseId,
                target.Release.ReleaseNumber,
                target.Release.SchemaVersion,
                target.Release.MenuCoreApiVersion,
                target.ServerKey,
                target.ServerGroupKey,
                target.ArtifactJson,
                target.Checksum,
                target.CapabilityManifestJson,
                target.Release.PublishedAt))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    public async Task<MenuReleaseHeadSnapshot?> GetHeadAsync(
        string serverKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);

        await using var context = await contextFactory
            .CreateDbContextAsync(cancellationToken)
            .ConfigureAwait(false);

        return await context.ReleaseHeads
            .AsNoTracking()
            .Where(head => head.ServerKey == serverKey)
            .Select(head => new MenuReleaseHeadSnapshot(
                head.ServerKey,
                head.ReleaseId,
                head.LockVersion,
                head.UpdatedAt))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}
