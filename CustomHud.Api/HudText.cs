using System.Net;

namespace CustomHud.Api;

/// <summary>Подготавливает пользовательские значения для вставки в HUD-разметку.</summary>
public static class HudText
{
    /// <summary>Экранирует HTML и цветовые теги, например в нике игрока; применять перед подстановкой в шаблон.</summary>
    public static string Escape(string value) => WebUtility.HtmlEncode(value)
        .Replace("[", "&#91;", StringComparison.Ordinal)
        .Replace("]", "&#93;", StringComparison.Ordinal);
}
