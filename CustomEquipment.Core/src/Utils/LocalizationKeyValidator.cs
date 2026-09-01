namespace CustomEquipment.Utils;

internal static class LocalizationKeyValidator
{
    private const int MaximumLength = 191;

    public static string Required(string? value, string field)
    {
        return Optional(value, field)
               ?? throw new InvalidOperationException($"{field} cannot be empty.");
    }

    public static string? Optional(string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var key = value.Trim();

        if (key.Length is < 2 or > MaximumLength ||
            !char.IsAsciiLetterOrDigit(key[0]) ||
            key.Skip(1).Any(character =>
                !char.IsAsciiLetterOrDigit(character) &&
                character is not '_' and not '.' and not '-'))
        {
            throw new InvalidOperationException($"{field} is not a valid localization key.");
        }

        return key;
    }
}
