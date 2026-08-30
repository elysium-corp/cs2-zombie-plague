using SwiftlyS2.Shared.Menus;

namespace Menu.Core.Swiftly;

/// <summary>
/// Разбирает переносимый список известных Swiftly key flags без числовых enum-значений.
/// </summary>
internal static class SwiftlyButtonParser
{
    internal static bool TryParse(string? value, out KeyBind button)
    {
        button = 0;
        if (string.IsNullOrWhiteSpace(value) || value.Length > 64)
        {
            return false;
        }

        foreach (var token in value.Split('+'))
        {
            if (token.Length == 0
                || token.Any(static character => !char.IsAsciiLetterOrDigit(character))
                || !Enum.TryParse<KeyBind>(token, ignoreCase: true, out var parsed)
                || !Enum.IsDefined(parsed)
                || parsed == 0)
            {
                button = 0;
                return false;
            }

            button |= parsed;
        }

        return button != 0;
    }
}
