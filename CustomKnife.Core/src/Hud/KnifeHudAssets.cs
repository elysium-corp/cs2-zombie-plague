using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace CustomKnife.Hud;

/// <summary>Контракт ресурсов Flute CMS: пути Source 2 и соответствующие классы экспортируемого CSS.</summary>
internal static partial class KnifeHudAssets
{
    internal const string IconConstraint = "hud_icon_path IS NULL OR hud_icon_path ~ '^panorama/images/([a-z0-9_-]+/)*[a-z0-9_-]+\\.vsvg$'";
    internal const string PreviewConstraint = "hud_preview_path IS NULL OR hud_preview_path ~ '^panorama/images/([a-z0-9_-]+/)*[a-z0-9_-]+_png\\.vtex$'";

    public static string? Validate(string? path, bool preview)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (path.Length > 512 || !(preview ? PreviewPath() : IconPath()).IsMatch(path))
            throw new InvalidOperationException("Некорректный путь ресурса Knife HUD: " + path);
        return path;
    }

    public static string CssClass(string path, bool preview)
        => (preview ? "CmsPreview_custom_" : "CmsIcon_custom_")
            + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(Validate(path, preview)!)))[..32];

    [GeneratedRegex(@"\Apanorama/images/(?:[a-z0-9_-]+/)*[a-z0-9_-]+\.vsvg\z", RegexOptions.CultureInvariant)]
    private static partial Regex IconPath();

    [GeneratedRegex(@"\Apanorama/images/(?:[a-z0-9_-]+/)*[a-z0-9_-]+_png\.vtex\z", RegexOptions.CultureInvariant)]
    private static partial Regex PreviewPath();
}
