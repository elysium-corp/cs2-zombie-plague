using CS2ZombiePlague.Di;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;

namespace CS2ZombiePlague.Utils.Helpers;

public static class RenderColorHelper
{
    /// <summary>
    /// Сбрасывает цвет визуализации (Render color) у всех валидных игроков
    /// до стандартного белого значения (255, 255, 255).
    /// </summary>
    public static void AllResetRenderColor()
    {
        var core = DependencyManager.GetService<ISwiftlyCore>();
        var players = core.PlayerManager.GetAllValidPlayers();
        
        foreach (var player in players)
        {
            player.RequiredPlayerPawn.Render = new Color(255, 255, 255);
            player.RequiredPlayerPawn.AnimatedEveryTickUpdated();
        }
    }
}