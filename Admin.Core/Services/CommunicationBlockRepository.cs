using Admin.Core.Data;
using Admin.Core.Database;
using Admin.Core.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Admin.Core.Services;

internal sealed class CommunicationBlockRepository(IDbContextFactory<AdminDbContext> factory)
{
    public async Task<List<CommunicationBlockEntity>> LoadAsync(CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var now = DateTime.UtcNow;
        return await context.CommunicationBlocks.AsNoTracking()
            .Where(block => block.ExpiresAtUtc == null || block.ExpiresAtUtc > now)
            .ToListAsync(cancellationToken);
    }

    public async Task SaveAsync(ulong steamId, CommunicationKind kind, ulong? administrator,
        DateTime? expiresAt, string reason, bool remove, CancellationToken cancellationToken)
    {
        await using var context = await factory.CreateDbContextAsync(cancellationToken);
        var id = checked((long)steamId);
        var type = (int)kind;
        if (remove)
        {
            await context.CommunicationBlocks.Where(block => block.SteamId == id && block.Kind == kind)
                .ExecuteDeleteAsync(cancellationToken);
            return;
        }

        long? actor = administrator.HasValue ? checked((long)administrator.Value) : null;
        var now = DateTime.UtcNow;
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO admin.communication_blocks (steam_id, kind, administrator_steam_id, expires_at, reason, updated_at)
            VALUES ({id}, {type}, {actor}, {expiresAt}, {reason}, {now})
            ON CONFLICT (steam_id, kind) DO UPDATE SET
                administrator_steam_id = EXCLUDED.administrator_steam_id,
                expires_at = EXCLUDED.expires_at, reason = EXCLUDED.reason, updated_at = EXCLUDED.updated_at
            """, cancellationToken);
    }
}
