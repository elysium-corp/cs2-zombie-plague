namespace ZPCore.Utils.Helpers;

internal static class HtmlHelper
{ 
    /// <summary>
    /// Оборачивает переданный текст в HTML-тег font с указанным цветом.
    /// Используется для отображения цветного текста в UI/чате.
    /// </summary>
    /// <param name="source">Текст, который необходимо окрасить.</param>
    /// <param name="colorTag">
    /// Цвет в формате HEX (#RRGGBB), RGB или название цвета,
    /// поддерживаемое системой отображения.
    /// </param>
    /// <returns>Строка с применённым цветовым форматированием.</returns>
    public static string TextWithColor(string source, string colorTag)
    {
        return $"<font color='{colorTag}'>{source}</font>";
    }
}