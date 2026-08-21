using Admin.Core.Data;
using Admin.Core.Database;
using Admin.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Services;

/// <summary>
/// Реализует persistent-хранение и получение блокировок игроков.
/// </summary>
internal sealed class BanService(IDbContextFactory<AdminDbContext> dbContextFactory) : IBanService
{
    /// <inheritdoc />
    public async Task BanAsync(
        ulong steamId,
        ulong? bannedBySteamId,
        TimeSpan? duration,
        string reason,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);

        if (steamId == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(steamId));
        }

        if (duration.HasValue && duration.Value <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        var now = DateTime.UtcNow;
        var databaseSteamId = checked((long)steamId);
        DateTime? expiresAtUtc = duration.HasValue ? now.Add(duration.Value) : null;

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        var ban = await context.Bans
            .SingleOrDefaultAsync(x => x.SteamId == databaseSteamId, cancellationToken)
            .ConfigureAwait(false);

        if (ban is null)
        {
            ban = new BanEntity
            {
                SteamId = databaseSteamId,
                CreatedAtUtc = now
            };

            context.Bans.Add(ban);
        }

        ban.BannedBySteamId = bannedBySteamId.HasValue ? checked((long)bannedBySteamId.Value) : null;
        ban.Reason = reason.Trim();
        ban.ExpiresAtUtc = expiresAtUtc;
        ban.UpdatedAtUtc = now;

        await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ActiveBan?> FindActiveAsync(ulong steamId, CancellationToken cancellationToken = default)
    {
        if (steamId == 0)
        {
            return null;
        }

        var databaseSteamId = checked((long)steamId);
        var now = DateTime.UtcNow;

        await using var context = await dbContextFactory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);

        return await context.Bans
            .AsNoTracking()
            .Where(x => x.SteamId == databaseSteamId && (x.ExpiresAtUtc == null || x.ExpiresAtUtc > now))
            .Select(x => new ActiveBan(
                x.Reason,
                x.ExpiresAtUtc
            ))
            .SingleOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);
    }
}