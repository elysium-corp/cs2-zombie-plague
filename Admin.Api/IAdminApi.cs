using Admin.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Admin.Api;

/// <summary>
/// Предоставляет публичный API административной системы.
///
/// Позволяет получать определения привилегий
/// и проверять права подключённых игроков.
/// </summary>
public interface IAdminApi
{
    /// <summary>
    /// Ищет определение привилегии по её ключу.
    /// </summary>
    /// <param name="key">
    /// Ключ привилегии в формате <c>group.id</c>.
    /// </param>
    /// <returns>
    /// Привилегию либо <c>null</c>, если она отсутствует
    /// в текущем runtime-каталоге.
    /// </returns>
    IPrivilege? FindPrivilege(string key);

    /// <summary>
    /// Возвращает все привилегии из текущего runtime-каталога.
    /// </summary>
    IReadOnlyCollection<IPrivilege> GetPrivileges();

    /// <summary>
    /// Возвращает все активные привилегии игрока.
    /// </summary>
    /// <remarks>
    /// Метод работает только с runtime-состоянием
    /// и не выполняет запрос в базу данных.
    /// </remarks>
    IReadOnlyCollection<IPrivilege> GetPlayerPrivileges(IPlayer player);

    /// <summary>
    /// Проверяет наличие активной привилегии у игрока.
    /// </summary>
    bool HasPrivilege(IPlayer player, string privilegeKey);

    /// <summary>
    /// Проверяет наличие разрешения у игрока.
    /// </summary>
    bool HasPermission(IPlayer player, string permission);

    /// <summary>
    /// Ключ Shared Interface.
    /// </summary>
    public static readonly string SharedApiKey = "Admin.Api.IAdminApi";
}