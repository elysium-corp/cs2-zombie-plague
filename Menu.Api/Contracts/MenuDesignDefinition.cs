using System.Text.Json;
using System.Text.Json.Serialization;

namespace Menu.Api.Contracts;

/// <summary>Описывает визуальные и навигационные параметры Swiftly Menu adapter.</summary>
public sealed record MenuDesignDefinition
{
    /// <summary>Разрешает игроку закрыть меню.</summary>
    [JsonPropertyName("canClose")]
    public bool CanClose { get; init; } = true;

    /// <summary>Замораживает игрока на время показа меню.</summary>
    [JsonPropertyName("freezePlayer")]
    public bool FreezePlayer { get; init; }

    /// <summary>Включает welcome screen, если capability доступна.</summary>
    [JsonPropertyName("welcomeScreen")]
    public bool WelcomeScreen { get; init; }

    /// <summary>Включает режим overlay-only, если capability доступна.</summary>
    [JsonPropertyName("overlayOnly")]
    public bool OverlayOnly { get; init; }

    /// <summary>Разрешает показывать отключённые пункты.</summary>
    [JsonPropertyName("showDisabledItems")]
    public bool ShowDisabledItems { get; init; } = true;

    /// <summary>Включает переход к родительскому меню.</summary>
    [JsonPropertyName("parentNavigation")]
    public bool ParentNavigation { get; init; } = true;

    /// <summary>Включает циклическую прокрутку пунктов.</summary>
    [JsonPropertyName("wrapNavigation")]
    public bool WrapNavigation { get; init; }

    /// <summary>Число пунктов страницы либо <c>null</c> для значения adapter.</summary>
    [JsonPropertyName("itemsPerPage")]
    public int? ItemsPerPage { get; init; }

    /// <summary>Cooldown прокрутки в миллисекундах либо <c>null</c>.</summary>
    [JsonPropertyName("scrollCooldownMilliseconds")]
    public int? ScrollCooldownMilliseconds { get; init; }

    /// <summary>Переопределение цвета в формате, поддерживаемом adapter.</summary>
    [JsonPropertyName("overrideColor")]
    public string? OverrideColor { get; init; }

    /// <summary>Ключ или путь безопасно настроенного звука меню.</summary>
    [JsonPropertyName("menuSound")]
    public string? MenuSound { get; init; }

    /// <summary>Локализуемые системные подписи меню.</summary>
    [JsonPropertyName("texts")]
    public MenuDesignTextDefinition Texts { get; init; } = new();

    /// <summary>Пользовательские назначения клавиш.</summary>
    [JsonPropertyName("keyBindings")]
    public IReadOnlyList<MenuKeyBindingDefinition> KeyBindings { get; init; }
        = Array.Empty<MenuKeyBindingDefinition>();

    /// <summary>Дополнительные кнопки меню.</summary>
    [JsonPropertyName("extraButtons")]
    public IReadOnlyList<MenuExtraButtonDefinition> ExtraButtons { get; init; }
        = Array.Empty<MenuExtraButtonDefinition>();

    /// <summary>Версионируемые дополнительные параметры adapter без секретов.</summary>
    [JsonPropertyName("options")]
    public IReadOnlyDictionary<string, JsonElement> Options { get; init; }
        = new Dictionary<string, JsonElement>();
}

/// <summary>Содержит локализуемые системные подписи Swiftly Menu design.</summary>
public sealed record MenuDesignTextDefinition
{
    /// <summary>Заголовок экрана проверки доступа.</summary>
    [JsonPropertyName("accessTitle")]
    public LocalizedText? AccessTitle { get; init; }

    /// <summary>Шаблон заголовка меню.</summary>
    [JsonPropertyName("menuTitle")]
    public LocalizedText? MenuTitle { get; init; }

    /// <summary>Сообщение об отсутствии доступа.</summary>
    [JsonPropertyName("noAccess")]
    public LocalizedText? NoAccess { get; init; }

    /// <summary>Подпись следующей страницы.</summary>
    [JsonPropertyName("nextPage")]
    public LocalizedText? NextPage { get; init; }

    /// <summary>Подпись предыдущей страницы.</summary>
    [JsonPropertyName("previousPage")]
    public LocalizedText? PreviousPage { get; init; }

    /// <summary>Шаблон выбранного пункта.</summary>
    [JsonPropertyName("currentlySelected")]
    public LocalizedText? CurrentlySelected { get; init; }

    /// <summary>Центральный текст меню.</summary>
    [JsonPropertyName("centerMenuText")]
    public LocalizedText? CenterMenuText { get; init; }

    /// <summary>Шаблон центрального имени свойства.</summary>
    [JsonPropertyName("centerMenuProperty")]
    public LocalizedText? CenterMenuProperty { get; init; }

    /// <summary>Шаблон центрального значения свойства.</summary>
    [JsonPropertyName("centerMenuValue")]
    public LocalizedText? CenterMenuValue { get; init; }
}

/// <summary>Описывает безопасное назначение клавиши на действие меню.</summary>
public sealed record MenuKeyBindingDefinition
{
    /// <summary>Стабильный ASCII-ключ назначения.</summary>
    [JsonPropertyName("bindingKey")]
    public string BindingKey { get; init; } = string.Empty;

    /// <summary>Имя кнопки, известное Swiftly Menu API.</summary>
    [JsonPropertyName("button")]
    public string Button { get; init; } = string.Empty;

    /// <summary>
    /// Действие при нажатии для пользовательского назначения.
    /// Для select/forward/backward/exit должно иметь kind=none: эти клавиши выполняют
    /// встроенную навигацию Swiftly и не могут одновременно быть ExtraButton.
    /// </summary>
    [JsonPropertyName("action")]
    public MenuActionDefinition Action { get; init; } = new();
}

/// <summary>Описывает дополнительную кнопку меню.</summary>
public sealed record MenuExtraButtonDefinition
{
    /// <summary>Стабильный ASCII-ключ кнопки.</summary>
    [JsonPropertyName("buttonKey")]
    public string ButtonKey { get; init; } = string.Empty;

    /// <summary>Локализуемая подпись.</summary>
    [JsonPropertyName("label")]
    public LocalizedText Label { get; init; } = new();

    /// <summary>Имя назначенной клавиши.</summary>
    [JsonPropertyName("button")]
    public string Button { get; init; } = string.Empty;

    /// <summary>Действие кнопки.</summary>
    [JsonPropertyName("action")]
    public MenuActionDefinition Action { get; init; } = new();
}
