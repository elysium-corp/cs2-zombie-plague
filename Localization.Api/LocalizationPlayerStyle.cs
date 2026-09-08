namespace Localization.Api;

/// <summary>Оформление наиболее приоритетной активной роли получателя; без роли используются обычные цвета.</summary>
/// <param name="RoleKey">Ключ роли или пустая строка.</param>
/// <param name="RoleName">Название роли или пустая строка.</param>
/// <param name="ChatColor">Цвет из палитры чата SwiftlyS2.</param>
/// <param name="HudColor">HTML-цвет #RRGGBB.</param>
public sealed record LocalizationPlayerStyle(string RoleKey = "", string RoleName = "",
    string ChatColor = "default", string HudColor = "#ffffff");
