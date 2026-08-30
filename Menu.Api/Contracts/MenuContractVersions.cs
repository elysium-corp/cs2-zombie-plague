namespace Menu.Api.Contracts;

/// <summary>
/// Содержит версии и общие ограничения публичного Menu API v1.
/// </summary>
public static class MenuContractVersions
{
    /// <summary>Текущая версия публичного API интеграции Provider.</summary>
    public const int MenuCoreApiVersion = 1;

    /// <summary>Текущая версия нормализованной JSON-схемы меню.</summary>
    public const int SchemaVersion = 1;

    /// <summary>Максимальная длина технического идентификатора.</summary>
    public const int MaxTechnicalIdentifierLength = 128;

    /// <summary>
    /// Регулярное выражение ASCII-идентификатора, используемое валидатором Menu.Core.
    /// </summary>
    /// <remarks>
    /// DTO намеренно не применяют выражение в setter или конструкторе. Полная
    /// проверка выполняется Menu.Core перед регистрацией, публикацией и активацией.
    /// </remarks>
    public const string TechnicalIdentifierPattern = "^[a-z0-9][a-z0-9._-]{0,127}$";
}

/// <summary>
/// Содержит стабильные ключи возможностей Swiftly Menu adapter.
/// </summary>
public static class MenuFeatureKeys
{
    /// <summary>Пункты Checkbox.</summary>
    public const string Checkbox = "checkbox";

    /// <summary>Пункты Slider.</summary>
    public const string Slider = "slider";

    /// <summary>Пункты Choice.</summary>
    public const string Choice = "choice";

    /// <summary>Пункты C4.</summary>
    public const string C4 = "c4";

    /// <summary>Пользовательские назначения клавиш.</summary>
    public const string CustomKeyBinds = "customKeyBinds";

    /// <summary>Переход к родительскому меню.</summary>
    public const string ParentNavigation = "parentNavigation";

    /// <summary>Дополнительные кнопки меню.</summary>
    public const string ExtraButtons = "extraButtons";

    /// <summary>Overlay-only режим.</summary>
    public const string OverlayOnly = "overlayOnly";

    /// <summary>Стартовый экран меню.</summary>
    public const string WelcomeScreen = "welcomeScreen";

    /// <summary>Циклическая навигация menu options.</summary>
    public const string WrapNavigation = "wrapNavigation";

    /// <summary>Индивидуальный cooldown прокрутки меню.</summary>
    public const string ScrollCooldown = "scrollCooldown";

    /// <summary>Общее переопределение цветов дизайна.</summary>
    public const string OverrideColor = "overrideColor";

    /// <summary>Индивидуальный путь звука меню.</summary>
    public const string MenuSound = "menuSound";

    /// <summary>Отдельный заголовок экрана проверки доступа.</summary>
    public const string AccessTitle = "accessTitle";

    /// <summary>Переопределение подписи следующей страницы.</summary>
    public const string NextPageText = "nextPageText";

    /// <summary>Переопределение подписи предыдущей страницы.</summary>
    public const string PreviousPageText = "previousPageText";

    /// <summary>Переопределение шаблона выбранного пункта.</summary>
    public const string CurrentlySelectedText = "currentlySelectedText";

    /// <summary>Переопределение центральных текстовых шаблонов.</summary>
    public const string CenterMenuText = "centerMenuText";

    /// <summary>Включение и отключение стандартных звуков Swiftly.</summary>
    public const string SoundToggle = "soundToggle";

    /// <summary>Автоматическое закрытие меню по таймеру.</summary>
    public const string AutoClose = "autoClose";
}

/// <summary>
/// Содержит стабильные ключи возможностей Provider.
/// </summary>
public static class MenuProviderCapabilityKeys
{
    /// <summary>Provider экспортирует программные меню.</summary>
    public const string OpenMenu = "open_menu";

    /// <summary>Provider экспортирует валидируемые действия.</summary>
    public const string Actions = "actions";
}
