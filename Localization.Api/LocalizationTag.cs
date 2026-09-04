namespace Localization.Api;

/// <summary>
/// Описывает локализованный тег, полученный из текущего snapshot Localization.Core.
/// </summary>
/// <param name="Key">Стабильный ключ тега без префикса <c>Tags.</c>.</param>
/// <param name="Text">Текст тега для запрошенного языка с fallback на язык сервера.</param>
/// <param name="Color">Поддерживаемый цвет SwiftlyS2.</param>
public sealed record LocalizationTag(
    string Key,
    string Text,
    string Color);
