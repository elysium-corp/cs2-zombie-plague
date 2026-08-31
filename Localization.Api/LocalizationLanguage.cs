namespace Localization.Api;

/// <summary>
/// Описывает доступный игроку язык локализации.
/// </summary>
/// <param name="Code">Нормализованный код языка.</param>
/// <param name="Name">Название языка для администраторов.</param>
/// <param name="NativeName">Самоназвание языка для игрового меню.</param>
/// <param name="SortOrder">Порядок отображения.</param>
public sealed record LocalizationLanguage(
    string Code,
    string Name,
    string NativeName,
    int SortOrder);
