namespace Menu.Api.Hud;

/// <summary>
/// Описывает Panorama layout и серверные обработчики интерактивного HUD-меню.
/// </summary>
public sealed class HudMenuDefinition
{
    private readonly Dictionary<string, HudMenuButtonHandler> _buttons = new(StringComparer.Ordinal);

    /// <summary>
    /// Создаёт описание HUD-меню.
    /// </summary>
    /// <param name="id">Уникальный идентификатор меню.</param>
    /// <param name="layoutPath">Путь к скомпилированному Panorama XML внутри подключённого VPK.</param>
    /// <param name="rootPanelId">Идентификатор корневой панели, управляющей видимостью меню.</param>
    /// <param name="openClassName">CSS-класс, добавляемый корневой панели при открытии.</param>
    public HudMenuDefinition(
        string id,
        string layoutPath,
        string rootPanelId,
        string openClassName = "is-open")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        ArgumentException.ThrowIfNullOrWhiteSpace(layoutPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(rootPanelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(openClassName);

        Id = id;
        LayoutPath = layoutPath;
        RootPanelId = rootPanelId;
        OpenClassName = openClassName;
    }

    /// <summary>
    /// Уникальный идентификатор меню.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Путь к Panorama XML внутри подключённого VPK.
    /// </summary>
    public string LayoutPath { get; }

    /// <summary>
    /// Идентификатор корневой панели меню.
    /// </summary>
    public string RootPanelId { get; }

    /// <summary>
    /// CSS-класс открытого состояния корневой панели.
    /// </summary>
    public string OpenClassName { get; }

    /// <summary>
    /// Зарегистрированные обработчики, индексированные по идентификатору кнопки.
    /// </summary>
    public IReadOnlyDictionary<string, HudMenuButtonHandler> Buttons => _buttons;

    /// <summary>
    /// Добавляет серверный обработчик кнопки из Panorama XML.
    /// </summary>
    /// <param name="buttonId">Значение атрибута <c>id</c> кнопки.</param>
    /// <param name="handler">Обработчик нажатия.</param>
    /// <returns>Текущее описание для последовательной настройки.</returns>
    /// <exception cref="InvalidOperationException">Обработчик этой кнопки уже зарегистрирован.</exception>
    public HudMenuDefinition AddButton(string buttonId, HudMenuButtonHandler handler)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(buttonId);
        ArgumentNullException.ThrowIfNull(handler);

        if (!_buttons.TryAdd(buttonId, handler))
        {
            throw new InvalidOperationException(
                $"HUD menu '{Id}' already contains button '{buttonId}'."
            );
        }

        return this;
    }
}
