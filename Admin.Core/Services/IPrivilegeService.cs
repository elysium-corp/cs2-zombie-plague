using Admin.Api.Data;

namespace Admin.Core.Services;

/// <summary>
/// Выполняет runtime-проверки привилегий и разрешений игроков.
/// </summary>
/// <remarks>
/// Сервис не выполняет запросы в базу данных.
/// Все проверки производятся по данным, находящимся в
/// <see cref="Store.IPlayerPrivilegeStore"/>.
/// </remarks>
internal interface IPrivilegeService
{
    /// <summary>
    /// Возвращает все активные зарегистрированные привилегии игрока.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    IReadOnlyCollection<IPrivilege> GetPrivileges(ulong steamId);

    /// <summary>
    /// Проверяет наличие активной зарегистрированной привилегии.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">Ключ привилегии.</param>
    bool HasPrivilege(ulong steamId, string privilegeKey);

    /// <summary>
    /// Проверяет наличие разрешения хотя бы в одной активной
    /// зарегистрированной привилегии игрока.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="permission">Ключ разрешения.</param>
    bool HasPermission(ulong steamId, string permission);
}