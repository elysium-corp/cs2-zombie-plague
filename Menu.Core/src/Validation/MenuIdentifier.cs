using System.Text;
using System.Text.RegularExpressions;
using Menu.Api.Enums;

namespace Menu.Core.Validation;

internal static partial class MenuIdentifier
{
    private const int MaximumAliasLength = 64;

    public static bool IsTechnicalKey(string? value)
    {
        return value is not null && TechnicalKeyRegex().IsMatch(value);
    }

    public static bool IsPermission(string? value)
    {
        return value is not null && PermissionRegex().IsMatch(value);
    }

    public static string CanonicalizeAlias(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value
            .Trim()
            .Normalize(NormalizationForm.FormC)
            .ToLowerInvariant()
            .Normalize(NormalizationForm.FormC);
    }

    public static bool IsAliasValid(MenuCommandKind kind, string? alias)
    {
        if (string.IsNullOrWhiteSpace(alias))
        {
            return false;
        }

        var canonical = CanonicalizeAlias(alias);
        if (canonical.Length is 0 or > MaximumAliasLength || canonical.Any(char.IsControl))
        {
            return false;
        }

        return kind switch
        {
            MenuCommandKind.Chat => ChatAliasRegex().IsMatch(canonical),
            MenuCommandKind.Console => ConsoleAliasRegex().IsMatch(canonical),
            _ => false
        };
    }

    public static string CommandLookupKey(MenuCommandKind kind, string alias)
    {
        return string.Concat(((byte)kind).ToString(System.Globalization.CultureInfo.InvariantCulture), ":", CanonicalizeAlias(alias));
    }

    [GeneratedRegex("^[a-z0-9][a-z0-9._-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex TechnicalKeyRegex();

    [GeneratedRegex("^[a-z0-9][a-z0-9._:-]{0,127}$", RegexOptions.CultureInvariant)]
    private static partial Regex PermissionRegex();

    [GeneratedRegex("^[!/][^\\s]{1,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex ChatAliasRegex();

    [GeneratedRegex("^(?:sw_)?[a-z0-9][a-z0-9_]{0,63}$", RegexOptions.CultureInvariant)]
    private static partial Regex ConsoleAliasRegex();
}
