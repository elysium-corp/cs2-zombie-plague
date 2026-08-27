using Menu.Api.Extensions;
using Menu.Api.Hud;

namespace Menu.Api;

/// <summary>
/// Предоставляет общие API текстовых меню и интерактивного Custom HUD.
/// </summary>
public interface IMenuApi
{
    /// <summary>
    /// Получает диспетчер расширений существующих текстовых меню.
    /// </summary>
    IMenuExtensionDispatcher Dispatcher { get; }

    /// <summary>
    /// Получает реестр расширений существующих текстовых меню.
    /// </summary>
    IMenuExtensionRegistry Extensions { get; }

    /// <summary>
    /// Получает API интерактивных меню на базе <c>custom_hud_layout</c>.
    /// </summary>
    IHudMenuApi Hud { get; }
    
    /// <summary>
    /// Ключ публикации shared-интерфейса Menu.Api.
    /// </summary>
    static readonly string SharedApiKey = "Menu.Api.IMenuApi";
}
