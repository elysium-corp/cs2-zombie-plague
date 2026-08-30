using System.Net;
using Menu.Api.Contracts;

namespace Menu.Core.Swiftly;

/// <summary>
/// Разрешает локализованный текст и экранирует разметку перед передачей Swiftly.
/// </summary>
internal static class MenuTextRenderer
{
    internal static string Render(LocalizedText? text, string? locale, int maxLength = 512)
    {
        if (text is null)
        {
            return string.Empty;
        }

        var value = Resolve(text, locale);
        if (value.Length > maxLength)
        {
            var safeLength = maxLength;
            if (safeLength > 0
                && char.IsHighSurrogate(value[safeLength - 1])
                && safeLength < value.Length
                && char.IsLowSurrogate(value[safeLength]))
            {
                safeLength--;
            }

            value = value[..safeLength];
        }

        // Swiftly помещает строки в HTML renderer. Текст конфигурации является
        // данными, а не доверенной разметкой, поэтому теги и entities экранируются.
        return WebUtility.HtmlEncode(value);
    }

    internal static string Resolve(LocalizedText text, string? locale)
    {
        if (!string.IsNullOrWhiteSpace(locale))
        {
            if (text.Translations.TryGetValue(locale, out var exact))
            {
                return exact;
            }

            var separator = locale.IndexOfAny(['-', '_']);
            var language = separator > 0 ? locale[..separator] : locale;
            var match = text.Translations.FirstOrDefault(pair =>
                pair.Key.Equals(language, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrEmpty(match.Key))
            {
                return match.Value;
            }
        }

        return text.Default;
    }
}
