using CustomHud.Api;

namespace Advertisement.Core.Data;

/// <summary>Параметры вывода из CMS или экспортированного fallback; null сохраняет старую локальную настройку.</summary>
internal sealed record AdvertisementPresentation(
    string DisplayType,
    string? HudLocalizationKey,
    string HudPosition = "bottom_left",
    double HudDurationSeconds = 8,
    string HudStyle = "notice")
{
    internal HudBannerTemplate? Template { get; init; }
    internal string? HeaderKey { get; init; }
    internal string? TitleKey { get; init; }
    internal IReadOnlyDictionary<string, string> Parameters { get; init; } = new Dictionary<string, string>();
}
