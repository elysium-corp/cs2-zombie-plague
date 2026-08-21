using Admin.Core.Data;

namespace Admin.Core.Store;

/// <summary>
/// Хранит runtime-состояние назначений привилегий игроков в памяти процесса.
/// </summary>
/// <remarks>
/// Хранилище не является persistent и очищается при отключении игрока
/// или перезапуске сервера.
/// </remarks>
internal interface IPlayerPrivilegeStore
{
    /// <summary>
    /// Возвращает текущее runtime-состояние назначений игрока.
    /// </summary>
    /// <returns>
    /// Словарь, где ключом является ключ привилегии.
    /// Если состояние отсутствует, возвращается пустой словарь.
    /// </returns>
    IReadOnlyDictionary<string, PlayerPrivilege> Get(ulong steamId);

    /// <summary>
    /// Полностью заменяет runtime-набор назначений указанного игрока.
    /// </summary>
    void Set(ulong steamId, IEnumerable<PlayerPrivilege> privileges);

    /// <summary>
    /// Полностью удаляет runtime-состояние указанного игрока.
    /// </summary>
    void Remove(ulong steamId);
}