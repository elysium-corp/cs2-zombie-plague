using Admin.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Admin.Api;

/// <summary>
/// Предоставляет публичный API административной системы.
///
/// Позволяет регистрировать определения привилегий, проверять права игроков
/// и работать с сохранёнными назначениями привилегий.
/// </summary>
public interface IAdminApi
{
    /// <summary>
    /// Регистрирует новое определение привилегии.
    /// </summary>
    /// <param name="definition">Описание регистрируемой привилегии.</param>
    /// <returns>
    /// Зарегистрированную привилегию.
    ///
    /// Если привилегия с таким ключом уже зарегистрирована с тем же набором разрешений,
    /// возвращается существующий экземпляр.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Возникает, если идентификатор или группа привилегии пусты.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Возникает, если привилегия с таким ключом уже зарегистрирована,
    /// но содержит другой набор разрешений.
    /// </exception>
    IPrivilege RegisterPrivilege(PrivilegeDefinition definition);

    /// <summary>
    /// Ищет зарегистрированное определение привилегии по её ключу.
    /// </summary>
    /// <param name="key">
    /// Ключ привилегии в формате <c>group.id</c>.
    /// </param>
    /// <returns>
    /// Зарегистрированную привилегию либо <c>null</c>, если она неизвестна Registry.
    /// </returns>
    IPrivilege? FindPrivilege(string key);

    /// <summary>
    /// Возвращает все зарегистрированные определения привилегий.
    /// </summary>
    IReadOnlyCollection<IPrivilege> GetPrivileges();

    /// <summary>
    /// Возвращает все активные и зарегистрированные привилегии игрока,
    /// находящиеся в runtime-хранилище сервера.
    /// </summary>
    /// <param name="player">Игрок, привилегии которого необходимо получить.</param>
    /// <returns>
    /// Коллекцию активных привилегий игрока.
    /// </returns>
    /// <remarks>
    /// Истёкшие назначения и назначения с неизвестными Registry ключами
    /// в результат не попадают.
    ///
    /// Метод работает с runtime-состоянием и не выполняет запрос в базу данных.
    /// </remarks>
    IReadOnlyCollection<IPrivilege> GetPlayerPrivileges(IPlayer player);

    /// <summary>
    /// Проверяет наличие конкретной активной привилегии у игрока.
    /// </summary>
    /// <param name="player">Проверяемый игрок.</param>
    /// <param name="privilegeKey">
    /// Ключ привилегии в формате <c>group.id</c>.
    /// </param>
    /// <returns>
    /// <c>true</c>, если привилегия зарегистрирована, назначена игроку
    /// и срок её действия не истёк; иначе <c>false</c>.
    /// </returns>
    bool HasPrivilege(IPlayer player, string privilegeKey);

    /// <summary>
    /// Проверяет, предоставляет ли хотя бы одна активная привилегия игрока
    /// указанное разрешение.
    /// </summary>
    /// <param name="player">Проверяемый игрок.</param>
    /// <param name="permission">
    /// Ключ разрешения, например <c>admin.kick</c>.
    /// </param>
    /// <returns>
    /// <c>true</c>, если разрешение предоставляется хотя бы одной
    /// активной привилегией игрока; иначе <c>false</c>.
    /// </returns>
    bool HasPermission(IPlayer player, string permission);

    /// <summary>
    /// Создаёт или обновляет сохранённое назначение привилегии игроку.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">
    /// Ключ зарегистрированной привилегии.
    /// </param>
    /// <param name="expiresAtUtc">
    /// Время окончания действия в UTC или <c>null</c> для бессрочного назначения.
    /// </param>
    /// <returns>
    /// <c>true</c>, если назначение успешно сохранено.
    ///
    /// <c>false</c>, если SteamID некорректен, привилегия не зарегистрирована
    /// либо указан уже прошедший срок действия.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Возникает, если ключ пуст или <paramref name="expiresAtUtc"/>
    /// содержит время не в UTC.
    /// </exception>
    Task<bool> GrantPrivilegeAsync(
        ulong steamId,
        string privilegeKey,
        DateTime? expiresAtUtc = null);

    /// <summary>
    /// Удаляет сохранённое назначение привилегии у игрока.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">Ключ привилегии.</param>
    /// <returns>
    /// <c>true</c>, если существующее назначение было удалено;
    /// иначе <c>false</c>.
    /// </returns>
    Task<bool> RevokePrivilegeAsync(ulong steamId, string privilegeKey);

    /// <summary>
    /// Получает сохранённое назначение конкретной привилегии игроку.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">Ключ привилегии.</param>
    /// <returns>
    /// Информацию о сохранённом назначении либо <c>null</c>,
    /// если такого назначения нет.
    /// </returns>
    /// <remarks>
    /// В отличие от <see cref="GetPlayerPrivileges"/>, данный метод обращается
    /// к persistent-хранилищу и может вернуть уже истёкшее назначение.
    /// </remarks>
    Task<PlayerPrivilegeInfo?> FindPlayerPrivilegeAsync(
        ulong steamId,
        string privilegeKey);

    /// <summary>
    /// Продлевает срок действия существующей временной привилегии.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">Ключ зарегистрированной привилегии.</param>
    /// <param name="duration">Положительный интервал продления.</param>
    /// <returns>
    /// <c>true</c>, если срок действия был продлён;
    /// иначе <c>false</c>.
    /// </returns>
    /// <remarks>
    /// Активное назначение продлевается от текущего <c>ExpiresAtUtc</c>.
    ///
    /// Истёкшее назначение продлевается от текущего времени UTC.
    ///
    /// Бессрочное назначение продлить нельзя.
    /// </remarks>
    Task<bool> ExtendPrivilegeAsync(
        ulong steamId,
        string privilegeKey,
        TimeSpan duration);

    /// <summary>
    /// Ключ Shared Interface, используемый для получения <see cref="IAdminApi"/>
    /// другими SwiftlyS2-модулями.
    /// </summary>
    public static readonly string SharedApiKey = "Admin.Api.IAdminApi";
}