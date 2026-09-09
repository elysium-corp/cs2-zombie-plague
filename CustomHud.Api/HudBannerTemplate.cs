using SwiftlyS2.Shared.Players;

namespace CustomHud.Api;

/// <summary>Расширенный API баннеров. Все вызовы выполняются на игровом потоке.</summary>
public interface ICustomBannerApi
{
    /// <summary>Ключ дополнительного интерфейса; обычный ICustomHudApi остаётся совместимым.</summary>
    public const string SharedApiKey = "CustomHud.Api.ICustomBannerApi";
    /// <summary>Готов ли клиентский ресурс v4 к отправке баннеров.</summary>
    bool IsAvailable { get; }
    /// <summary>Показывает подготовленные поля. HTML допускает тот же ограниченный набор тегов, что и ICustomHudApi.</summary>
    bool Show(IPlayer player, HudBannerTemplate template, HudBannerContent content, HudMessageOptions? options = null);
    /// <summary>Разрешает ключи полей через Localization, экранирует параметры до подстановки и показывает баннер. false при отсутствии перевода или API Localization.</summary>
    bool ShowLocalized(IPlayer player, HudBannerTemplate template, HudBannerContent keys,
        IReadOnlyDictionary<string, object?> parameters, HudMessageOptions? options = null, string? language = null);
}

/// <summary>Поля баннера либо ключи Localization при вызове ShowLocalized. Неиспользуемые поля должны быть пустыми.</summary>
public sealed record HudBannerContent
{
    /// <summary>Короткий надзаголовок, одна строка.</summary>
    public string? Header { get; init; }
    /// <summary>Заголовок, одна строка.</summary>
    public string? Title { get; init; }
    /// <summary>Основной текст или описание, до четырёх строк.</summary>
    public string? Description { get; init; }
}

/// <summary>Сериализуемый дизайн баннера, одинаковый для API, CMS и fallback. Значения регистрозависимы; см. документацию конструктора.</summary>
public sealed record HudBannerTemplate
{
    /// <summary>Версия схемы дизайна. Текущий контракт — 1 (не версия клиентского ресурса v4).</summary>
    public int SchemaVersion { get; init; } = 1;
    /// <summary>Вариант: text, icon, headline, feature, hero либо custom с независимыми блоками.</summary>
    public string Variant { get; init; } = "headline";
    /// <summary>Тема: midnight, glass, solid, light, danger, transparent.</summary>
    public string Theme { get; init; } = "midnight";
    /// <summary>Акцент: имя цвета или #RGB / #RRGGBB; ближайший цвет палитры Panorama.</summary>
    public string Accent { get; init; } = "mint";
    /// <summary>Ширина: small, medium, large.</summary>
    public string Width { get; init; } = "medium";
    /// <summary>Размер текста: small, medium, large.</summary>
    public string Size { get; init; } = "medium";
    /// <summary>Выравнивание: left, center, right.</summary>
    public string Align { get; init; } = "left";
    /// <summary>Положение текста рядом с иконкой: top, center, bottom. Для иконки сверху не применяется.</summary>
    public string VerticalAlign { get; init; } = "center";
    /// <summary>Выравнивание надзаголовка: inherit использует Align; иначе left, center или right.</summary>
    public string HeaderAlign { get; init; } = "inherit";
    /// <summary>Выравнивание заголовка: inherit использует Align; иначе left, center или right.</summary>
    public string TitleAlign { get; init; } = "inherit";
    /// <summary>Выравнивание всех строк описания: inherit использует Align; иначе left, center или right.</summary>
    public string DescriptionAlign { get; init; } = "inherit";
    /// <summary>Граница: none, line, frame.</summary>
    public string Border { get; init; } = "line";
    /// <summary>Углы: square, soft, round.</summary>
    public string Corners { get; init; } = "soft";
    /// <summary>Иконка из VPK: none, info, warning, infection, skull, shield, trophy, star, gift, megaphone, lightning, clock, heart.</summary>
    public string Icon { get; init; } = "none";
    /// <summary>Размещение иконки: left или top.</summary>
    public string IconPosition { get; init; } = "left";
    /// <summary>Анимация иконки: none, pulse, spin, spin_reverse, bounce, shake, breathe, blink, glow, neon, shimmer, float, wobble, heartbeat.</summary>
    public string IconAnimation { get; init; } = "none";
    /// <summary>Появление: fade, slide_up/down/left/right, zoom, zoom_out, pop, flip_x/y, rotate, drop, swing, bounce_in либо none.</summary>
    public string Enter { get; init; } = "fade";
    /// <summary>Исчезновение: тот же набор, что Enter. После TTL допускается завершающая анимация до 0,8 секунды.</summary>
    public string Exit { get; init; } = "fade";
    /// <summary>Скорость эффектов: fast (0,2 с), normal (0,4 с), slow (0,8 с).</summary>
    public string Speed { get; init; } = "normal";
    /// <summary>Циклический эффект всего баннера, независимо от появления и исчезновения.</summary>
    public string ContainerAnimation { get; init; } = "none";
    /// <summary>Циклический эффект надзаголовка.</summary>
    public string HeaderAnimation { get; init; } = "none";
    /// <summary>Циклический эффект заголовка.</summary>
    public string TitleAnimation { get; init; } = "none";
    /// <summary>Циклический эффект описания.</summary>
    public string DescriptionAnimation { get; init; } = "none";
    /// <summary>Циклический эффект только подставленных параметров Localization и span.hud-parameter.</summary>
    public string ParameterAnimation { get; init; } = "none";
    /// <summary>Скорость цикла: fast (1,2 с), normal (2,4 с), slow (3,6 с).</summary>
    public string LoopSpeed { get; init; } = "normal";
    /// <summary>Подсветка иконки цветом акцента: none, soft, strong.</summary>
    public string IconGlow { get; init; } = "none";
    /// <summary>Подсветка текста цветом акцента: none, soft, strong.</summary>
    public string TextGlow { get; init; } = "none";
    /// <summary>Цвет параметров без явного цвета HTML/Localization: inherit наследует цвет блока, accent использует акцент. Явный цвет фрагмента всегда сохраняется.</summary>
    public string ParameterColor { get; init; } = "inherit";
    /// <summary>Задержка старта циклических эффектов, 0–2000 мс, шаг 100.</summary>
    public int EffectDelay { get; init; }

    /// <summary>Имя установленного sound event; null отключает звук. Проигрывается один раз при первом показе одному клиенту.</summary>
    public string? Sound { get; init; }
    /// <summary>Громкость от 0 до 1.</summary>
    public float Volume { get; init; } = 0.5f;

    /// <summary>Показывать надзаголовок в варианте custom.</summary>
    public bool ShowHeader { get; init; } = true;
    /// <summary>Показывать заголовок в варианте custom.</summary>
    public bool ShowTitle { get; init; } = true;
    /// <summary>Показывать описание в варианте custom. Нужен хотя бы один текстовый блок.</summary>
    public bool ShowDescription { get; init; } = true;
    /// <summary>Точная ширина в координатах 1920 × 1080: 320–960, шаг 40. null использует Width.</summary>
    public int? WidthPixels { get; init; }
    /// <summary>Внутренний отступ 0–40, шаг 4. null сохраняет отступ темы.</summary>
    public int? Padding { get; init; }
    /// <summary>Расстояние между блоками 0–24, шаг 2. null сохраняет стандартное.</summary>
    public int? Gap { get; init; }
    /// <summary>Размер надзаголовка 10–24, шаг 2. null использует Size.</summary>
    public int? HeaderSize { get; init; }
    /// <summary>Размер заголовка 16–48, шаг 2. null использует Size.</summary>
    public int? TitleSize { get; init; }
    /// <summary>Размер описания 12–32, шаг 2. null использует Size.</summary>
    public int? DescriptionSize { get; init; }
    /// <summary>Размер иконки 24–96, шаг 8. null означает 64.</summary>
    public int? IconSize { get; init; }
    /// <summary>Фон: theme, slate, black, blue, purple, red, green, gold, white.</summary>
    public string Background { get; init; } = "theme";
    /// <summary>Непрозрачность фона 0–100, шаг 10. Текст и иконка остаются непрозрачными.</summary>
    public int BackgroundOpacity { get; init; } = 100;
    /// <summary>Цвет надзаголовка: inherit, white, muted, mint, gold, red, green, blue, cyan, purple, pink, black.</summary>
    public string HeaderColor { get; init; } = "inherit";
    /// <summary>Цвет заголовка из того же набора. Явные HTML-цвета сохраняются.</summary>
    public string TitleColor { get; init; } = "inherit";
    /// <summary>Цвет описания из того же набора. Явные HTML-цвета сохраняются.</summary>
    public string DescriptionColor { get; init; } = "inherit";
    /// <summary>Тень: default, none, soft, strong.</summary>
    public string Shadow { get; init; } = "default";
}

/// <summary>Общие правила состава баннера для клиентов API и конструктора.</summary>
public static class HudBannerFields
{
    /// <summary>Возвращает включённые поля в порядке отрисовки.</summary>
    public static string[] Get(HudBannerTemplate template) => template.Variant switch
    {
        "text" or "icon" => ["Description"],
        "headline" => ["Title", "Description"],
        "custom" => new[] { template.ShowHeader ? "Header" : null, template.ShowTitle ? "Title" : null,
            template.ShowDescription ? "Description" : null }.OfType<string>().ToArray(),
        _ => ["Header", "Title", "Description"]
    };
}
