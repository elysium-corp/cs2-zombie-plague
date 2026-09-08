namespace Localization.Api;

/// <summary>Определяет обработку разметки и параметров локализованного сообщения.</summary>
public enum LocalizationOutputMode
{
    /// <summary>Цветовые коды чата SwiftlyS2; HTML-оформление преобразуется в доступные цвета.</summary>
    Chat,
    /// <summary>Безопасный HTML HUD; значения параметров экранируются и отмечаются для анимации.</summary>
    Html,
    /// <summary>Исходная разметка с текстовыми значениями параметров.</summary>
    Raw,
}
