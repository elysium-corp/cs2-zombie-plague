using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Shop.Core.Hud;

/// <summary>Настройки каждой редкости с наследованием стандартного цвета и общих звуков.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record ShopHudRarityStyles
{
    [JsonPropertyName("Common")] public ShopHudRarityStyle Common { get; init; } = new();
    [JsonPropertyName("Uncommon")] public ShopHudRarityStyle Uncommon { get; init; } = new();
    [JsonPropertyName("Rare")] public ShopHudRarityStyle Rare { get; init; } = new();
    [JsonPropertyName("Restricted")] public ShopHudRarityStyle Restricted { get; init; } = new();
    [JsonPropertyName("Classified")] public ShopHudRarityStyle Classified { get; init; } = new();
    [JsonPropertyName("Elite")] public ShopHudRarityStyle Elite { get; init; } = new();
    [JsonPropertyName("Prototype")] public ShopHudRarityStyle Prototype { get; init; } = new();
    [JsonPropertyName("Legendary")] public ShopHudRarityStyle Legendary { get; init; } = new();
    internal bool Valid => Common is { Valid: true } && Uncommon is { Valid: true } && Rare is { Valid: true } && Restricted is { Valid: true } && Classified is { Valid: true } && Elite is { Valid: true } && Prototype is { Valid: true } && Legendary is { Valid: true };
}

/// <summary>Ограниченный набор параметров, безопасно экспортируемых в Panorama CSS.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record ShopHudRarityStyle
{
    public string Color { get; init; } = "";
    public bool Glow { get; init; } = true;
    public int GlowWidth { get; init; } = 5;
    public int GlowIntensity { get; init; } = 100;
    public int BorderWidth { get; init; }
    public int FillOpacity { get; init; }
    public string HoverSound { get; init; } = "";
    public string ClickSound { get; init; } = "";

    internal bool Valid => Color is not null && (Color.Length == 0 || Regex.IsMatch(Color, @"\A#[0-9a-fA-F]{6}\z"))
        && GlowWidth is >= 0 and <= 16 && GlowIntensity is >= 0 and <= 200
        && BorderWidth is >= 0 and <= 4 && FillOpacity is >= 0 and <= 100
        && ValidSound(HoverSound) && ValidSound(ClickSound);

    internal static bool ValidSound(string? value) => value is not null && value.Length <= 128
        && (value.Length == 0 || Regex.IsMatch(value, @"\A[A-Za-z0-9_][A-Za-z0-9_.-]*\z"));
}
