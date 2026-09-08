using System.Text.RegularExpressions;

namespace Advertisement.Core.Application;

internal static class AdvertisementChatText
{
    private static readonly Regex Tags = new("<[^>]*>", RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);
    private static readonly Regex Breaks = new(@"<br\s*/?>", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.NonBacktracking);

    // После изменения общего ключа HTML остаётся читаемым текстом; стили чата сохраняются.
    internal static string? Normalize(string? text) => text is null ? null
        : Tags.Replace(Breaks.Replace(text, " "), string.Empty);
}
