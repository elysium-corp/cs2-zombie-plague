using System.Text.RegularExpressions;
using CustomHud.Api;

namespace CustomHud.Core;

internal sealed record HudBannerDocument(HudBannerTemplate Template, HudRun[] Header, HudRun[] Title);

internal static class HudBannerDesign
{
    internal static double Seconds(HudBannerTemplate template) => template.Speed switch { "fast" => 0.2, "slow" => 0.8, _ => 0.4 };

    internal static void Validate(HudBannerTemplate template, HudBannerContent content)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(content);
        if (template.SchemaVersion != 1) throw new ArgumentException("HUD: неизвестная версия схемы шаблона");
        Choice(template.Variant, "text", "icon", "headline", "feature", "hero");
        Choice(template.Theme, "midnight", "glass", "solid", "light", "danger", "transparent");
        Choice(template.Width, "small", "medium", "large");
        Choice(template.Size, "small", "medium", "large");
        Choice(template.Align, "left", "center", "right");
        Choice(template.Border, "none", "line", "frame");
        Choice(template.Corners, "square", "soft", "round");
        Choice(template.Icon, "none", "info", "warning", "infection", "skull", "shield", "trophy", "star", "gift", "megaphone", "lightning", "clock", "heart");
        Choice(template.IconPosition, "left", "top");
        Choice(template.IconAnimation, "none", "pulse", "spin", "bounce", "shake");
        Choice(template.Enter, "none", "fade", "slide_up", "slide_down", "slide_left", "slide_right", "zoom");
        Choice(template.Exit, "none", "fade", "slide_up", "slide_down", "slide_left", "slide_right", "zoom");
        Choice(template.Speed, "fast", "normal", "slow");
        if (!HudPalette.TryResolve(template.Accent, out _)) throw new ArgumentException("HUD: неизвестный цвет акцента");
        if (!float.IsFinite(template.Volume) || template.Volume is < 0 or > 1) throw new ArgumentException("HUD: громкость должна быть от 0 до 1");
        if (template.Sound is not null && (template.Sound.Length > 128 || !Regex.IsMatch(template.Sound, @"\A[A-Za-z0-9_][A-Za-z0-9_.]*\z", RegexOptions.CultureInvariant)))
            throw new ArgumentException("HUD: укажите имя установленного sound event");
        var title = template.Variant is "headline" or "feature" or "hero";
        var header = template.Variant is "feature" or "hero";
        if (string.IsNullOrWhiteSpace(content.Description) || (title && string.IsNullOrWhiteSpace(content.Title)) || (header && string.IsNullOrWhiteSpace(content.Header)))
            throw new ArgumentException("HUD: заполните все поля выбранного варианта");
        if ((!title && !string.IsNullOrEmpty(content.Title)) || (!header && !string.IsNullOrEmpty(content.Header)))
            throw new ArgumentException("HUD: вариант не использует переданные заголовки");
        if (template.Variant is "icon" or "feature" && template.Icon == "none") throw new ArgumentException("HUD: этому варианту нужна иконка");
        if (template.Variant == "text" && template.Icon != "none") throw new ArgumentException("HUD: текстовый вариант не использует иконку");
    }

    internal static HudDocument Parse(HudBannerTemplate template, HudBannerContent content, HudTextFormat format)
    {
        Validate(template, content);
        var width = template.Width == "small" ? 32 : template.Width == "large" ? 52 : 40;
        var description = HudMarkup.Parse(content.Description!, format, HudMessageStyle.Notice, width: width);
        var header = HudMarkup.Parse(content.Header ?? "", format, HudMessageStyle.Notice, 1, width).Lines.FirstOrDefault() ?? [];
        var title = HudMarkup.Parse(content.Title ?? "", format, HudMessageStyle.Banner, 1, width).Lines.FirstOrDefault() ?? [];
        return description with { Banner = new(template, header, title) };
    }

    internal static string[] Classes(HudBannerTemplate t)
    {
        HudPalette.TryResolve(t.Accent, out var accent);
        return ["CustomBanner", "Variant_" + t.Variant, "Theme_" + t.Theme, "A" + accent,
            "Width_" + t.Width, "Size_" + t.Size, "Align_" + t.Align, "Border_" + t.Border,
            "Corners_" + t.Corners, "Icon_" + t.Icon, "IconPosition_" + t.IconPosition,
            "IconAnimation_" + t.IconAnimation, "Enter_" + t.Enter, "Exit_" + t.Exit, "Speed_" + t.Speed];
    }

    private static void Choice(string value, params string[] allowed)
    {
        if (!allowed.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("HUD: неизвестное значение дизайна " + value);
    }
}
