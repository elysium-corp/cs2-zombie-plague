namespace Advertisement.Core.Data;

/// <summary>Параметры вывода из CMS или экспортированного fallback; null сохраняет старую локальную настройку.</summary>
internal sealed record AdvertisementPresentation(
    string DisplayType,
    string? HudLocalizationKey,
    string HudPosition = "bottom_left",
    double HudDurationSeconds = 8,
    string HudStyle = "notice");
