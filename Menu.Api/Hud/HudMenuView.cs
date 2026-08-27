namespace Menu.Api.Hud;

/// <summary>
/// Описывает персональные строки, CSS-состояния и бизнес-контекст открываемого HUD-меню.
/// </summary>
public sealed class HudMenuView
{
    private readonly Dictionary<(string PanelId, string VariableName), HudMenuDialogVariable> _variables = [];
    private readonly Dictionary<(string PanelId, string ClassName), HudMenuPanelClass> _classes = [];

    /// <summary>
    /// Персональные значения Panorama dialog variables.
    /// </summary>
    public IReadOnlyCollection<HudMenuDialogVariable> Variables => _variables.Values;

    /// <summary>
    /// Персональные состояния CSS-классов панелей.
    /// </summary>
    public IReadOnlyCollection<HudMenuPanelClass> Classes => _classes.Values;

    /// <summary>
    /// Пользовательское состояние, доступное обработчику нажатия кнопки.
    /// </summary>
    public object? State { get; private set; }

    /// <summary>
    /// Устанавливает значение Panorama dialog variable для панели.
    /// </summary>
    /// <param name="panelId">Идентификатор панели.</param>
    /// <param name="variableName">Имя переменной из выражения вида <c>{s:name}</c>.</param>
    /// <param name="value">Отображаемое значение.</param>
    /// <returns>Текущее представление для последовательной настройки.</returns>
    public HudMenuView SetDialogVariable(string panelId, string variableName, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(panelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(variableName);
        ArgumentNullException.ThrowIfNull(value);

        _variables[(panelId, variableName)] = new HudMenuDialogVariable(panelId, variableName, value);
        return this;
    }

    /// <summary>
    /// Задаёт наличие CSS-класса у панели для конкретного игрока.
    /// </summary>
    /// <param name="panelId">Идентификатор панели.</param>
    /// <param name="className">Имя CSS-класса.</param>
    /// <param name="enabled">Нужно ли добавить класс.</param>
    /// <returns>Текущее представление для последовательной настройки.</returns>
    public HudMenuView SetClass(string panelId, string className, bool enabled)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(panelId);
        ArgumentException.ThrowIfNullOrWhiteSpace(className);

        _classes[(panelId, className)] = new HudMenuPanelClass(panelId, className, enabled);
        return this;
    }

    /// <summary>
    /// Прикрепляет бизнес-состояние к сессии открытого меню.
    /// </summary>
    /// <param name="state">Состояние, которое получит обработчик кнопки.</param>
    /// <returns>Текущее представление для последовательной настройки.</returns>
    public HudMenuView WithState(object? state)
    {
        State = state;
        return this;
    }
}

/// <summary>
/// Персональное значение Panorama dialog variable.
/// </summary>
/// <param name="PanelId">Идентификатор панели.</param>
/// <param name="VariableName">Имя переменной.</param>
/// <param name="Value">Отображаемое значение.</param>
public sealed record HudMenuDialogVariable(string PanelId, string VariableName, string Value);

/// <summary>
/// Персональное состояние CSS-класса панели.
/// </summary>
/// <param name="PanelId">Идентификатор панели.</param>
/// <param name="ClassName">Имя CSS-класса.</param>
/// <param name="Enabled">Нужно ли добавить класс.</param>
public sealed record HudMenuPanelClass(string PanelId, string ClassName, bool Enabled);
