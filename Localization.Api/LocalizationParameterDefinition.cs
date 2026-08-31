namespace Localization.Api;

/// <summary>
/// Поддерживаемые типы параметров шаблона локализации.
/// </summary>
public enum LocalizationParameterType
{
    /// <summary>
    /// Произвольная строка.
    /// </summary>
    String,

    /// <summary>
    /// Целое 64-битное число.
    /// </summary>
    Integer,

    /// <summary>
    /// Число с плавающей точкой.
    /// </summary>
    Number,

    /// <summary>
    /// Логическое значение <c>true</c> или <c>false</c>.
    /// </summary>
    Boolean,
}

/// <summary>
/// Описывает один типизированный параметр шаблона локализации.
/// </summary>
public sealed record LocalizationParameterDefinition
{
    /// <summary>
    /// Создаёт описание параметра шаблона.
    /// </summary>
    /// <param name="name">Имя без фигурных скобок.</param>
    /// <param name="type">Тип принимаемого значения.</param>
    /// <param name="isRequired">Признак обязательного значения.</param>
    /// <param name="description">Подсказка для разработчиков и администраторов.</param>
    /// <param name="example">Пример, используемый в предпросмотре.</param>
    public LocalizationParameterDefinition(
        string name,
        LocalizationParameterType type,
        bool isRequired,
        string? description,
        string example)
    {
        Name = name;
        Type = type;
        IsRequired = isRequired;
        Description = description;
        Example = example;
    }

    /// <summary>
    /// Имя параметра без фигурных скобок.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Тип принимаемого значения.
    /// </summary>
    public LocalizationParameterType Type { get; init; }

    /// <summary>
    /// Показывает, обязательно ли передавать значение при форматировании.
    /// </summary>
    public bool IsRequired { get; init; }

    /// <summary>
    /// Возвращает описание назначения параметра.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// Возвращает строковый пример значения для предпросмотра.
    /// </summary>
    public string Example { get; init; }
}
