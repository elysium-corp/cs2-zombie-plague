using CustomHud.Api;
using Microsoft.EntityFrameworkCore;

namespace MapRotation.Core.Database;

/// <summary>Личные настройки меню по SteamID; операции вызываются только фоновыми задачами.</summary>
internal interface IRotationHudPreferenceStore
{
    /// <summary>Читает сохранённые настройки или возвращает null для нового игрока.</summary>
    Task<HudMenuPresentation?> LoadAsync(ulong steamId, CancellationToken cancellationToken);
    /// <summary>Атомарно сохраняет ориентацию, масштаб, край и анимацию игрока.</summary>
    Task SaveAsync(ulong steamId, HudMenuPresentation presentation, CancellationToken cancellationToken);
}

internal sealed class RotationHudPreferenceStore(IDbContextFactory<MapRotationDbContext> factory) : IRotationHudPreferenceStore
{
    public async Task<HudMenuPresentation?> LoadAsync(ulong steamId, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var id = checked((long)steamId);
        var row = await db.PlayerPreferences.AsNoTracking().SingleOrDefaultAsync(x => x.SteamId == id, cancellationToken).ConfigureAwait(false);
        return row is null ? null : new()
        {
            Orientation = row.Orientation == "vertical" ? HudMenuOrientation.Vertical : HudMenuOrientation.Horizontal,
            ScalePercent = row.HudScale is 80 or 100 or 120 ? row.HudScale : 80,
            DockSide = row.DockSide == "left" ? HudMenuDockSide.Left : HudMenuDockSide.Right,
            Animation = row.Animation switch
            {
                "none" => HudMenuAnimation.None,
                "fast" => HudMenuAnimation.Fast,
                "slow" => HudMenuAnimation.Slow,
                _ => HudMenuAnimation.Normal
            }
        };
    }

    public async Task SaveAsync(ulong steamId, HudMenuPresentation presentation, CancellationToken cancellationToken)
    {
        await using var db = await factory.CreateDbContextAsync(cancellationToken).ConfigureAwait(false);
        var id = checked((long)steamId);
        var orientation = presentation.Orientation == HudMenuOrientation.Horizontal ? "horizontal" : "vertical";
        var dockSide = presentation.DockSide == HudMenuDockSide.Left ? "left" : "right";
        var animation = presentation.Animation.ToString().ToLowerInvariant();
        await db.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO map_rotation.player_preferences (steam_id, orientation, hud_scale, dock_side, animation)
            VALUES ({id}, {orientation}, {presentation.ScalePercent}, {dockSide}, {animation})
            ON CONFLICT (steam_id) DO UPDATE
            SET orientation = EXCLUDED.orientation, hud_scale = EXCLUDED.hud_scale,
                dock_side = EXCLUDED.dock_side, animation = EXCLUDED.animation;
            """, cancellationToken).ConfigureAwait(false);
    }
}
