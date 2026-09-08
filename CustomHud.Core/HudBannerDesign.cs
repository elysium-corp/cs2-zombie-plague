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
        Choice(template.Variant, "text", "icon", "headline", "feature", "hero", "custom");
        Choice(template.Theme, "midnight", "glass", "solid", "light", "danger", "transparent");
        Choice(template.Width, "small", "medium", "large");
        Choice(template.Size, "small", "medium", "large");
        Choice(template.Align, "left", "center", "right");
        Choice(template.VerticalAlign, "top", "center", "bottom");
        foreach (var align in new[] { template.HeaderAlign, template.TitleAlign, template.DescriptionAlign })
            Choice(align, "inherit", "left", "center", "right");
        Choice(template.Border, "none", "line", "frame");
        Choice(template.Corners, "square", "soft", "round");
        Choice(template.Icon, "none", "info", "warning", "infection", "skull", "shield", "trophy", "star", "gift", "megaphone", "lightning", "clock", "heart");
        Choice(template.IconPosition, "left", "top");
        Choice(template.IconAnimation, "none", "pulse", "breathe", "blink", "glow", "neon", "shimmer", "float", "bounce", "shake", "wobble", "heartbeat", "spin", "spin_reverse");
        Choice(template.Enter, "none", "fade", "slide_up", "slide_down", "slide_left", "slide_right", "zoom", "zoom_out", "flip_x", "flip_y", "rotate", "drop", "swing", "pop", "bounce_in");
        Choice(template.Exit, "none", "fade", "slide_up", "slide_down", "slide_left", "slide_right", "zoom", "zoom_out", "flip_x", "flip_y", "rotate", "drop", "swing", "pop", "bounce_in");
        Choice(template.ContainerAnimation, "none", "pulse", "breathe", "blink", "glow", "neon", "shimmer", "float", "bounce", "shake", "wobble", "heartbeat");
        Choice(template.HeaderAnimation, "none", "pulse", "breathe", "blink", "glow", "neon", "shimmer", "float", "bounce", "shake", "wobble", "heartbeat");
        Choice(template.TitleAnimation, "none", "pulse", "breathe", "blink", "glow", "neon", "shimmer", "float", "bounce", "shake", "wobble", "heartbeat");
        Choice(template.DescriptionAnimation, "none", "pulse", "breathe", "blink", "glow", "neon", "shimmer", "float", "bounce", "shake", "wobble", "heartbeat");
        Choice(template.ParameterAnimation, "none", "pulse", "breathe", "blink", "glow", "neon", "shimmer", "float", "bounce", "shake", "wobble", "heartbeat");
        Choice(template.LoopSpeed, "fast", "normal", "slow");
        Choice(template.IconGlow, "none", "soft", "strong");
        Choice(template.TextGlow, "none", "soft", "strong");
        Choice(template.ParameterColor, "inherit", "accent", "white", "muted", "mint", "gold", "red", "green", "blue", "cyan", "purple", "pink", "black");
        Choice(template.Speed, "fast", "normal", "slow");
        if (!HudPalette.TryResolve(template.Accent, out _)) throw new ArgumentException("HUD: неизвестный цвет акцента");
        if (!float.IsFinite(template.Volume) || template.Volume is < 0 or > 1) throw new ArgumentException("HUD: громкость должна быть от 0 до 1");
        if (template.Sound is not null && (template.Sound.Length > 128 || !Regex.IsMatch(template.Sound, @"\A[A-Za-z0-9_][A-Za-z0-9_.]*\z", RegexOptions.CultureInvariant)))
            throw new ArgumentException("HUD: укажите имя установленного sound event");
        Choice(template.Background, "theme", "slate", "black", "blue", "purple", "red", "green", "gold", "white");
        Choice(template.Shadow, "default", "none", "soft", "strong");
        foreach (var color in new[] { template.HeaderColor, template.TitleColor, template.DescriptionColor })
            Choice(color, "inherit", "white", "muted", "mint", "gold", "red", "green", "blue", "cyan", "purple", "pink", "black");
        Step(template.EffectDelay, 0, 2000, 100);
        Step(template.WidthPixels, 320, 960, 40); Step(template.Padding, 0, 40, 4); Step(template.Gap, 0, 24, 2);
        Step(template.HeaderSize, 10, 24, 2); Step(template.TitleSize, 16, 48, 2); Step(template.DescriptionSize, 12, 32, 2);
        Step(template.IconSize, 24, 96, 8); Step(template.BackgroundOpacity, 0, 100, 10);
        var fields = HudBannerFields.Get(template);
        if (fields.Length == 0) throw new ArgumentException("HUD: включите хотя бы один текстовый блок");
        foreach (var (name, value) in new[] { ("Header", content.Header), ("Title", content.Title), ("Description", content.Description) })
        {
            if (fields.Contains(name) ? string.IsNullOrWhiteSpace(value) : !string.IsNullOrEmpty(value))
                throw new ArgumentException("HUD: заполните только включённые поля выбранного варианта: " + name);
        }
        if (template.Variant is "icon" or "feature" && template.Icon == "none") throw new ArgumentException("HUD: этому варианту нужна иконка");
        if (template.Variant == "text" && template.Icon != "none") throw new ArgumentException("HUD: текстовый вариант не использует иконку");
    }

    internal static HudDocument Parse(HudBannerTemplate template, HudBannerContent content, HudTextFormat format)
    {
        Validate(template, content);
        var description = HudMarkup.Parse(content.Description ?? "", format, HudMessageStyle.Notice, width: TextWidth(template, "Description"));
        var header = HudMarkup.Parse(content.Header ?? "", format, HudMessageStyle.Notice, 1, TextWidth(template, "Header")).Lines.FirstOrDefault() ?? [];
        var title = HudMarkup.Parse(content.Title ?? "", format, HudMessageStyle.Banner, 1, TextWidth(template, "Title")).Lines.FirstOrDefault() ?? [];
        return description with { Banner = new(template, header, title) };
    }

    // Оценка строки учитывает место иконки, отступы и шрифт конкретного поля
    // Та же формула используется в Preview CMS; Panorama дополнительно уменьшает длинные глифы
    internal static int TextWidth(HudBannerTemplate template, string field)
    {
        var available = ContentWidth(template);
        var fontSize = (field, template.Size) switch
        {
            ("Header", "small") => 12, ("Header", "large") => 16, ("Header", _) => 14,
            ("Title", "small") => 24, ("Title", "large") => 36, ("Title", _) => 30,
            (_, "small") => 18, (_, "large") => 26, _ => 22
        };
        fontSize = (field switch { "Header" => template.HeaderSize, "Title" => template.TitleSize, _ => template.DescriptionSize }) ?? fontSize;
        var glyphWidth = fontSize * 0.65 + (field == "Header" ? 2 : 0);
        return Math.Clamp((int)(available / glyphWidth), 8, 80);
    }

    internal static int ContentWidth(HudBannerTemplate template)
    {
        var panelWidth = template.WidthPixels ?? (template.Width switch { "small" => 440, "large" => 760, _ => 600 });
        var available = panelWidth - (template.Padding is { } padding ? padding * 2 + 4 : 48)
            - (template.Icon != "none" && template.IconPosition == "left" ? (template.IconSize ?? 64) + 16 : 0);
        return Math.Clamp(available / 8 * 8, 128, 960);
    }

    internal static string[] Classes(HudBannerTemplate t)
    {
        HudPalette.TryResolve(t.Accent, out var accent);
        var classes = new List<string> { "CustomBanner", "TextWidth_" + ContentWidth(t), "Variant_" + t.Variant, "Theme_" + t.Theme, "A" + accent,
            "Width_" + t.Width, "Size_" + t.Size, "Align_" + t.Align, "Border_" + t.Border,
            "VerticalAlign_" + t.VerticalAlign, "HeaderAlign_" + t.HeaderAlign,
            "TitleAlign_" + t.TitleAlign, "DescriptionAlign_" + t.DescriptionAlign,
            "Corners_" + t.Corners, "Icon_" + t.Icon, "IconPosition_" + t.IconPosition,
            "IconAnimation_" + t.IconAnimation, "Enter_" + t.Enter, "Exit_" + t.Exit, "Speed_" + t.Speed,
            "Background_" + t.Background, "BackgroundOpacity_" + t.BackgroundOpacity,
            "HeaderColor_" + t.HeaderColor, "TitleColor_" + t.TitleColor, "DescriptionColor_" + t.DescriptionColor,
            "Shadow_" + t.Shadow, "EffectDelay_" + t.EffectDelay,
            "ContainerAnimation_" + t.ContainerAnimation,
            "HeaderAnimation_" + t.HeaderAnimation,
            "TitleAnimation_" + t.TitleAnimation,
            "DescriptionAnimation_" + t.DescriptionAnimation,
            "ParameterAnimation_" + t.ParameterAnimation,
            "LoopSpeed_" + t.LoopSpeed,
            "IconGlow_" + t.IconGlow,
            "TextGlow_" + t.TextGlow,
            "ParameterColor_" + t.ParameterColor };
        foreach (var (name, value) in new[] { ("WidthPixels", t.WidthPixels), ("Padding", t.Padding), ("Gap", t.Gap),
                     ("HeaderSize", t.HeaderSize), ("TitleSize", t.TitleSize), ("DescriptionSize", t.DescriptionSize), ("IconSize", t.IconSize) })
            if (value.HasValue) classes.Add(name + "_" + value.Value);
        var fields = HudBannerFields.Get(t);
        if (!fields.Contains("Description")) classes.Add("NoDescription");
        if (!fields.Contains("Title")) classes.Add("NoTitle");
        return classes.ToArray();
    }

    private static void Step(int? value, int minimum, int maximum, int step)
    {
        if (value is { } number && (number < minimum || number > maximum || (number - minimum) % step != 0))
            throw new ArgumentException("HUD: размер выходит за диапазон или не соответствует шагу");
    }

    private static void Choice(string value, params string[] allowed)
    {
        if (!allowed.Contains(value, StringComparer.Ordinal)) throw new ArgumentException("HUD: неизвестное значение дизайна " + value);
    }
}
