using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shop.Core.Hud;

/// <summary>Проверенные варианты оформления, реализованные одновременно в Panorama и Web-превью.</summary>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
internal sealed record ShopHudAppearance
{
    public string Theme { get; init; } = "elysium";
    public string Accent { get; init; } = "mint";
    public string OpenAnimation { get; init; } = "fade";
    public string CloseAnimation { get; init; } = "fade";
    public string PageAnimation { get; init; } = "fade";
    public string AnimationSpeed { get; init; } = "normal";
    public string Highlight { get; init; } = "rarity";
    public string IconStyle { get; init; } = "glow";
    public string NameAlignment { get; init; } = "left";
    public int Columns { get; init; } = 8;
    public int Rows { get; init; } = 6;
    public bool WrapPages { get; init; }
    public int Width { get; init; } = 1720;
    public int Height { get; init; } = 980;
    public int DefaultScale { get; init; } = 100;
    public string SelectionAnimation { get; init; } = "pulse";
    public string ClickBehavior { get; init; } = "buy";
    public int MaxPurchasesPerRound { get; init; }

    internal ShopHudAppearance WithFrame(ShopHudAppearance frame) => this with
    { Width = frame.Width, Height = frame.Height, DefaultScale = frame.DefaultScale };

    internal static readonly ShopHudAppearance Default = new();
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    internal static ShopHudAppearance Parse(string? json)
    {
        var value = JsonSerializer.Deserialize<ShopHudAppearance>(string.IsNullOrWhiteSpace(json) ? "{}" : json, JsonOptions)
            ?? throw new InvalidDataException("Оформление магазина должно быть JSON-объектом");
        if (!new[] { "elysium", "minimal", "tactical" }.Contains(value.Theme) ||
            !new[] { "mint", "gold", "violet", "blue", "red" }.Contains(value.Accent) ||
            !new[] { "none", "fade", "slide", "zoom" }.Contains(value.OpenAnimation) ||
            !new[] { "none", "fade", "slide", "zoom" }.Contains(value.CloseAnimation) ||
            !new[] { "none", "fade", "slide" }.Contains(value.PageAnimation) ||
            !new[] { "fast", "normal", "slow" }.Contains(value.AnimationSpeed) ||
            !new[] { "none", "rarity", "accent" }.Contains(value.Highlight) ||
            !new[] { "glow", "silhouette", "hidden" }.Contains(value.IconStyle) ||
            !new[] { "left", "center", "right" }.Contains(value.NameAlignment) ||
            !new[] { 960, 1120, 1280, 1440, 1600, 1720 }.Contains(value.Width) ||
            !new[] { 640, 760, 880, 980 }.Contains(value.Height) ||
            !ShopHudPreference.Scales.Contains(value.DefaultScale) ||
            !new[] { "none", "pulse", "lift" }.Contains(value.SelectionAnimation) ||
            !new[] { "buy", "buy_close", "confirm" }.Contains(value.ClickBehavior) ||
            !new[] { 0, 1, 2, 3, 5, 10 }.Contains(value.MaxPurchasesPerRound) ||
            value.Columns is < 1 or > ShopHudCatalog.ColumnCount || value.Rows is < 1 or > ShopHudCatalog.RowCount)
            throw new InvalidDataException("Недопустимый вариант оформления магазина");
        return value;
    }

    internal IEnumerable<(string Group, string Class)> Classes()
    {
        yield return ("width", "Width" + Width);
        yield return ("height", "Height" + Height);
        yield return ("selectionAnimation", "Selection_" + SelectionAnimation);
        yield return ("theme", "Theme_" + Theme);
        yield return ("accent", "Accent_" + Accent);
        yield return ("closeAnimation", "Close_" + CloseAnimation);
        yield return ("openAnimation", "Open_" + OpenAnimation);
        yield return ("pageAnimation", "Page_" + PageAnimation);
        yield return ("animationSpeed", "Speed_" + AnimationSpeed);
        yield return ("highlight", "Highlight_" + Highlight);
        yield return ("iconStyle", "Icons_" + IconStyle);
        yield return ("nameAlignment", "Names_" + NameAlignment);
    }

    internal double Duration => AnimationSpeed == "fast" ? 0.15 : AnimationSpeed == "slow" ? 0.6 : 0.3;

    internal static int MovePage(int page, int count, int direction, bool wrap) => count <= 1 ? 0
        : wrap ? (page + direction + count) % count : Math.Clamp(page + direction, 0, count - 1);
}
