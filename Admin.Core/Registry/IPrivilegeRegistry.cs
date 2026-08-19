using Admin.Api.Data;

namespace Admin.Core.Registry;

/// <summary>
/// Хранит определения привилегий, доступных административной системе.
/// </summary>
/// <remarks>
/// Ключи привилегий сравниваются без учёта регистра.
///
/// Registry содержит определения привилегий, а не назначения конкретным игрокам.
/// </remarks>
internal interface IPrivilegeRegistry
{
    /// <summary>
    /// Регистрирует определение привилегии.
    /// </summary>
    /// <param name="definition">Описание привилегии.</param>
    /// <returns>
    /// Новый зарегистрированный объект либо уже существующий объект,
    /// если повторная регистрация полностью совпадает по набору разрешений.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Идентификатор или группа пусты.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Такой ключ уже зарегистрирован с другим набором разрешений.
    /// </exception>
    IPrivilege Register(PrivilegeDefinition definition);

    /// <summary>
    /// Ищет зарегистрированную привилегию по ключу без учёта регистра.
    /// </summary>
    IPrivilege? Find(string key);

    /// <summary>
    /// Возвращает все зарегистрированные привилегии.
    /// </summary>
    IReadOnlyCollection<IPrivilege> GetAll();
}