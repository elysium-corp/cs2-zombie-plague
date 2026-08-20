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
    /// Полностью заменяет текущий runtime-набор определений привилегий.
    /// </summary>
    /// <param name="definitions">
    /// Актуальные определения привилегий из persistent-хранилища.
    /// </param>
    void ReplaceAll(IEnumerable<PrivilegeDefinition> definitions);

    /// <summary>
    /// Ищет зарегистрированную привилегию по ключу без учёта регистра.
    /// </summary>
    IPrivilege? Find(string key);

    /// <summary>
    /// Возвращает все зарегистрированные привилегии.
    /// </summary>
    IReadOnlyCollection<IPrivilege> GetAll();
}