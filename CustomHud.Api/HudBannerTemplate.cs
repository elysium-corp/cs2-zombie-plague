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
    /// <summary>Вариант: text, icon, headline, feature, hero.</summary>
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
    /// <summary>Граница: none, line, frame.</summary>
    public string Border { get; init; } = "line";
    /// <summary>Углы: square, soft, round.</summary>
    public string Corners { get; init; } = "soft";
    /// <summary>Иконка из VPK: none, info, warning, infection, skull, shield, trophy, star, gift, megaphone, lightning, clock, heart.</summary>
    public string Icon { get; init; } = "none";
    /// <summary>Размещение иконки: left или top.</summary>
    public string IconPosition { get; init; } = "left";
    /// <summary>Анимация иконки: none, pulse, spin, bounce, shake.</summary>
    public string IconAnimation { get; init; } = "none";
    /// <summary>Появление: none, fade, slide_up, slide_down, slide_left, slide_right, zoom.</summary>
    public string Enter { get; init; } = "fade";
    /// <summary>Исчезновение: тот же набор, что Enter. После TTL допускается завершающая анимация до 0,8 секунды.</summary>
    public string Exit { get; init; } = "fade";
    /// <summary>Скорость эффектов: fast (0,2 с), normal (0,4 с), slow (0,8 с).</summary>
    public string Speed { get; init; } = "normal";
    /// <summary>Имя установленного sound event; null отключает звук. Проигрывается один раз при первом показе одному клиенту.</summary>
    public string? Sound { get; init; }
    /// <summary>Громкость от 0 до 1.</summary>
    public float Volume { get; init; } = 0.5f;
}
