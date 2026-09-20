// Создано scripts/generate-knife-hud.py; редактируйте knife-hud-assets.json.
namespace CustomKnife.Hud;

internal static class KnifeHudImages
{
    private static readonly HashSet<string> Icons = new(StringComparer.Ordinal) { "butterfly", "falchion", "huntsman", "karambit", "knife", "m9_bayonet", "shadow_daggers", "skeleton" };
    private static readonly HashSet<string> Previews = new(StringComparer.Ordinal) { "butterfly", "falchion", "huntsman", "karambit", "knife", "m9_bayonet", "shadow_daggers", "skeleton" };
    public static string ResolveIcon(string? image, string knifeId) => Resolve(Icons, image, knifeId);
    public static string ResolvePreview(string? image, string knifeId) => Resolve(Previews, image, knifeId);

    private static string Resolve(HashSet<string> names, string? image, string knifeId)
    {
        if (!string.IsNullOrWhiteSpace(image)) return names.Contains(image) ? image : "knife";
        if (names.Contains(knifeId)) return knifeId;
        var shortName = knifeId.StartsWith("knife_", StringComparison.Ordinal) ? knifeId[6..] : knifeId;
        return names.Contains(shortName) ? shortName : "knife";
    }
}
