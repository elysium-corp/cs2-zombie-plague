namespace CustomHud.Api;

/// <summary>Оформление и срок жизни одного HUD-сообщения.</summary>
public sealed record HudMessageOptions
{
    /// <summary>Показывать отдельный экземпляр под предыдущими; до трёх карточек в области. При заполнении Show возвращает false.</summary>
    public bool Stack { get; init; }
    /// <summary>Канал владельца длиной 1–64 символа; разные плагины должны использовать разные имена.</summary>
    public string Channel { get; init; } = "default";

    /// <summary>Область экрана; одновременно показывается одно сообщение в каждой области.</summary>
    public HudPosition Position { get; init; } = HudPosition.TopCenter;

    /// <summary>Срок жизни от 0,5 до 60 секунд, включая время перекрытия более приоритетным сообщением.</summary>
    public double DurationSeconds { get; init; } = 6;

    /// <summary>Приоритет от 0 до 1000; при равном приоритете побеждает последнее сообщение.</summary>
    public int Priority { get; init; }

    /// <summary>Компактное уведомление или крупный баннер.</summary>
    public HudMessageStyle Style { get; init; } = HudMessageStyle.Notice;

    /// <summary>Разбор поддерживаемой разметки либо буквальный вывод текста.</summary>
    public HudTextFormat Format { get; init; } = HudTextFormat.Markup;
}

/// <summary>Области экрана относительно безопасной зоны Panorama.</summary>
public enum HudPosition
{
    /// <summary>Слева сверху.</summary>
    TopLeft,
    /// <summary>Сверху по центру, ниже стандартной полосы игроков.</summary>
    TopCenter,
    /// <summary>Справа сверху.</summary>
    TopRight,
    /// <summary>Слева посередине.</summary>
    MiddleLeft,
    /// <summary>В центре экрана.</summary>
    Center,
    /// <summary>Справа посередине.</summary>
    MiddleRight,
    /// <summary>Слева снизу.</summary>
    BottomLeft,
    /// <summary>Снизу по центру.</summary>
    BottomCenter,
    /// <summary>Справа снизу.</summary>
    BottomRight
}

/// <summary>Предустановленный стиль сообщения.</summary>
public enum HudMessageStyle
{
    /// <summary>Компактное рекламное или информационное уведомление.</summary>
    Notice,
    /// <summary>Крупный баннер с акцентной полосой Elysium.</summary>
    Banner
}

/// <summary>Способ интерпретации переданного текста.</summary>
public enum HudTextFormat
{
    /// <summary>Серверный разбор font color, span color, b, i, u, br и цветовых тегов [red]…[/].</summary>
    Markup,
    /// <summary>Буквальный текст без разбора тегов и HTML-сущностей.</summary>
    PlainText
}
