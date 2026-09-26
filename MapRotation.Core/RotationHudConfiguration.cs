using System.Text.Json;
using CustomHud.Api;

namespace MapRotation.Core;

/// <summary>Параметры HUD из CMS; интервалы выбирают готовые варианты скомпилированного оформления.</summary>
/// <param name="HideOnClose">Кнопка закрытия полностью скрывает голосование вместо сворачивания к краю.</param>
/// <param name="ShowResult">Показывать карточку победителя после голосования.</param>
internal sealed record RotationHudConfiguration(HudMenuPresentation Defaults, int ResultDuration, int VerticalGap,
    int HorizontalGap = 16, bool HideOnClose = false, bool ShowResult = true)
{
    public static RotationHudConfiguration Parse(string json)
    {
        var defaults = RotationHudPreferences.Default;
        var resultDuration = 6;
        var verticalGap = 24;
        var horizontalGap = 16;
        var hideOnClose = false;
        var showResult = true;
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return new(defaults, resultDuration, verticalGap);
            if (root.TryGetProperty("defaultDock", out var dock) && dock.ValueKind == JsonValueKind.String)
                defaults = defaults with { DockSide = dock.GetString() == "left" ? HudMenuDockSide.Left : HudMenuDockSide.Right };
            if (root.TryGetProperty("defaultAnimation", out var animation) && animation.ValueKind == JsonValueKind.String)
                defaults = defaults with { Animation = animation.GetString() switch
                {
                    "none" => HudMenuAnimation.None,
                    "fast" => HudMenuAnimation.Fast,
                    "slow" => HudMenuAnimation.Slow,
                    _ => HudMenuAnimation.Normal
                } };
            if (root.TryGetProperty("resultDuration", out var duration) && duration.ValueKind == JsonValueKind.Number
                && duration.TryGetInt32(out var seconds))
                resultDuration = Math.Clamp(seconds, 2, 15);
            if (root.TryGetProperty("verticalGap", out var gap) && gap.ValueKind == JsonValueKind.Number
                && gap.TryGetInt32(out var pixels))
                verticalGap = Math.Clamp(pixels, 0, 32);
            if (root.TryGetProperty("horizontalGap", out var columns) && columns.ValueKind == JsonValueKind.Number
                && columns.TryGetInt32(out var columnPixels))
                horizontalGap = Math.Clamp(columnPixels, 0, 32);
            if (root.TryGetProperty("voteClose", out var close) && close.ValueKind == JsonValueKind.String)
                hideOnClose = close.GetString() == "hide";
            if (root.TryGetProperty("showResult", out var result) && result.ValueKind is JsonValueKind.True or JsonValueKind.False)
                showResult = result.GetBoolean();
        }
        catch (JsonException)
        {
            // Повреждение оформления не должно отключать ротацию карт.
        }
        return new(defaults, resultDuration, verticalGap, horizontalGap, hideOnClose, showResult);
    }
}
