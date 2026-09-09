using Microsoft.EntityFrameworkCore;

namespace Shop.Core.Database;

/// <summary>Хранение персонального масштаба магазина по SteamID, независимо от каталога товаров.</summary>
internal interface IShopHudPreferenceStore
{
    /// <summary>Возвращает сохранённый масштаб либо null для нового игрока.</summary>
    Task<int?> LoadAsync(ulong steamId, CancellationToken cancellationToken);
    /// <summary>Атомарно создаёт или обновляет настройку игрока; ошибка не считается сохранением.</summary>
    Task SaveAsync(ulong steamId, int scalePercent, CancellationToken cancellationToken);
}

internal sealed class ShopHudPreferenceStore(IDbContextFactory<ShopDbContext> factory) : IShopHudPreferenceStore
{
    public async Task<int?> LoadAsync(ulong steamId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var id = checked((long)steamId);
        return await db.PlayerPreferences.AsNoTracking().Where(x => x.SteamId == id)
            .Select(x => (int?)x.HudScale).SingleOrDefaultAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task SaveAsync(ulong steamId, int scalePercent, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var id = checked((long)steamId);
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO shop.player_preferences (steam_id, hud_scale)
            VALUES ({id}, {scalePercent})
            ON CONFLICT (steam_id) DO UPDATE SET hud_scale = EXCLUDED.hud_scale;
            """, cancellationToken).ConfigureAwait(false);
    }
}
