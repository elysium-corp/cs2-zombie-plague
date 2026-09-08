namespace Shop.Core.Hud;

internal static class ShopHudIcons
{
    // Только штатные ресурсы CS2; значения каталога не превращаются в произвольные пути или CSS.
    internal static readonly IReadOnlySet<string> Names = new HashSet<string>(StringComparer.Ordinal)
    {
        "ak47", "aug", "awp", "bizon", "cz75a", "deagle", "elite", "famas", "fiveseven", "g3sg1",
        "galilar", "glock", "hkp2000", "m249", "m4a1", "m4a1_silencer", "mac10", "mag7", "mp5sd",
        "mp7", "mp9", "negev", "nova", "p250", "p90", "revolver", "sawedoff", "scar20", "sg556",
        "ssg08", "tec9", "ump45", "usp_silencer", "xm1014", "knife", "taser", "hegrenade",
        "flashbang", "smokegrenade", "molotov", "incgrenade", "decoy", "kevlar", "equipment"
    };

    internal static string Normalize(string name)
    {
        name = name.Trim().ToLowerInvariant();
        if (name.StartsWith("weapon_", StringComparison.Ordinal)) name = name[7..];
        return Names.Contains(name) ? name : "equipment";
    }
}
