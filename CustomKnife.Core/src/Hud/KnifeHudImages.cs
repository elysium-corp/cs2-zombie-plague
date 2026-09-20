// Создано scripts/generate-knife-hud.py; редактируйте knife-hud-assets.json.
namespace CustomKnife.Hud;

internal static class KnifeHudImages
{
    private static readonly HashSet<string> Names = new(StringComparer.Ordinal) { "knife" };
    public static string Resolve(string? image) => image is not null && Names.Contains(image) ? image : "knife";
}
