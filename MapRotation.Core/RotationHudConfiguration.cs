using System.Collections.Immutable;
using System.Text.Json;
using CustomHud.Api;

namespace MapRotation.Core;

/// <summary>Параметры HUD из CMS; интервалы выбирают готовые варианты скомпилированного оформления.</summary>
/// <param name="HideOnClose">Кнопка закрытия полностью скрывает голосование вместо сворачивания к краю.</param>
/// <param name="ShowResult">Показывать карточку победителя после голосования.</param>
/// <param name="AllowVoteChange">Разрешить менять голос; без разрешения первый выбор фиксируется, а меню сворачивается.</param>
internal sealed record RotationHudConfiguration(HudMenuPresentation Defaults, int ResultDuration, int VerticalGap,
    int HorizontalGap = 16, bool HideOnClose = false, bool ShowResult = true, bool AllowVoteChange = false)
{
    /// <summary>Анимации появления темы CMS; имя класса — ThemeEntrance и значение с заглавной буквы.</summary>
    private static readonly string[] Animations = ["none", "fade", "rise", "drop", "slideLeft", "slideRight", "zoom", "zoomOut", "pop", "tilt"];

    /// <summary>Классы ThemeX для ограниченных параметров оформления; все варианты скомпилированы в тему, поэтому смена не требует пересборки VPK.</summary>
    public ImmutableArray<string> ThemeClasses { get; init; } = [];

    public static RotationHudConfiguration Parse(string json)
    {
        var defaults = RotationHudPreferences.Default;
        var resultDuration = 6;
        var verticalGap = 24;
        var horizontalGap = 16;
        var hideOnClose = false;
        var showResult = true;
        var allowVoteChange = false;
        var themeClasses = ImmutableArray.CreateBuilder<string>();
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
            if (Flag(root, "voteChange") is { } voteChange) allowVoteChange = voteChange;
            ThemeClassesOf(root, themeClasses);
        }
        catch (JsonException)
        {
            // Повреждение оформления не должно отключать ротацию карт.
        }
        return new(defaults, resultDuration, verticalGap, horizontalGap, hideOnClose, showResult, allowVoteChange) { ThemeClasses = themeClasses.ToImmutable() };
    }

    // Отсутствующий или неверный параметр не даёт класса: остаётся значение, экспортированное в тему.
    private static void ThemeClassesOf(JsonElement root, ImmutableArray<string>.Builder classes)
    {
        if (root.TryGetProperty("animation", out var animation) && animation.ValueKind == JsonValueKind.String
            && Array.IndexOf(Animations, animation.GetString()) is >= 0 and var index)
            classes.Add("ThemeEntrance" + char.ToUpperInvariant(Animations[index][0]) + Animations[index][1..]);
        if (Integer(root, "duration") is { } duration)
            classes.Add("ThemeDuration" + (int)Math.Round(Math.Clamp(duration, 0, 800) / 10.0, MidpointRounding.AwayFromZero) * 10);
        foreach (var (key, name, min, max) in new[]
        {
            ("opacity", "Opacity", 40, 100), ("radius", "Radius", 0, 24), ("fontSize", "FontSize", 16, 32),
            ("width", "Width", 480, 900), ("imageWidth", "ImageWidth", 60, 240)
        })
            if (Integer(root, key) is { } value) classes.Add("Theme" + name + Math.Clamp(value, min, max));
        if (Flag(root, "showImages") is { } images) classes.Add(images ? "ThemeImagesOn" : "ThemeImagesOff");
        if (Flag(root, "showSubtitle") is { } subtitle) classes.Add(subtitle ? "ThemeSubtitleOn" : "ThemeSubtitleOff");
    }

    private static int? Integer(JsonElement root, string key)
        => root.TryGetProperty(key, out var value) && value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number) ? number : null;

    private static bool? Flag(JsonElement root, string key)
        => root.TryGetProperty(key, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False ? value.GetBoolean() : null;
}
