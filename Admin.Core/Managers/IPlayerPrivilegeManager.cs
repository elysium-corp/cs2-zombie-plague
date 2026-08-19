using Admin.Core.Data;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Managers;

/// <summary>
/// Управляет жизненным циклом назначений привилегий игроков.
///
/// Координирует загрузку persistent-состояния, runtime-хранилище,
/// изменение назначений и защиту от применения устаревших асинхронных результатов.
/// </summary>
internal interface IPlayerPrivilegeManager
{
    /// <summary>
    /// Инициализирует runtime-состояние привилегий подключившегося игрока.
    /// </summary>
    /// <param name="player">Подключённый и авторизованный игрок.</param>
    /// <remarks>
    /// Перед началом загрузки существующее runtime-состояние игрока очищается.
    ///
    /// Загрузка из persistent-хранилища выполняется асинхронно.
    /// Пока она не завершена, игрок считается не имеющим привилегий.
    /// </remarks>
    void Initialize(IPlayer player);

    /// <summary>
    /// Удаляет runtime-состояние игрока и делает результаты асинхронных операций
    /// предыдущей игровой сессии неактуальными.
    /// </summary>
    /// <param name="player">Отключающийся игрок.</param>
    void Remove(IPlayer player);

    /// <summary>
    /// Перезагружает назначения привилегий указанного онлайн-игрока
    /// из persistent-хранилища.
    /// </summary>
    /// <param name="steamId">SteamID64 онлайн-игрока.</param>
    /// <returns>
    /// <c>true</c>, если актуальные данные были загружены и применены;
    /// <c>false</c>, если игрок не имеет активной runtime-сессии
    /// или сессия изменилась во время загрузки.
    /// </returns>
    Task<bool> ReloadAsync(ulong steamId);

    /// <summary>
    /// Перезагружает назначения привилегий всех игроков,
    /// имеющих активную runtime-сессию.
    /// </summary>
    /// <remarks>
    /// Метод предназначен в том числе для синхронизации сервера
    /// с изменениями, внесёнными во внешнюю базу данных, например веб-панелью.
    /// </remarks>
    Task ReloadAllAsync();

    /// <summary>
    /// Ищет сохранённое назначение привилегии игрока.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">Ключ привилегии.</param>
    /// <returns>
    /// Назначение либо <c>null</c>, если оно отсутствует.
    ///
    /// Истёкшее назначение также может быть возвращено.
    /// </returns>
    Task<PlayerPrivilege?> FindAsync(ulong steamId, string privilegeKey);

    /// <summary>
    /// Создаёт или обновляет назначение зарегистрированной привилегии.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">Ключ зарегистрированной привилегии.</param>
    /// <param name="expiresAtUtc">
    /// Время окончания действия в UTC или <c>null</c> для бессрочного назначения.
    /// </param>
    /// <returns>
    /// <c>true</c>, если назначение успешно сохранено; иначе <c>false</c>.
    /// </returns>
    Task<bool> GrantAsync(
        ulong steamId,
        string privilegeKey,
        DateTime? expiresAtUtc = null);

    /// <summary>
    /// Удаляет сохранённое назначение привилегии.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">Ключ привилегии.</param>
    /// <returns>
    /// <c>true</c>, если существующее назначение было удалено;
    /// иначе <c>false</c>.
    /// </returns>
    Task<bool> RevokeAsync(ulong steamId, string privilegeKey);

    /// <summary>
    /// Продлевает существующее временное назначение привилегии.
    /// </summary>
    /// <param name="steamId">SteamID64 игрока.</param>
    /// <param name="privilegeKey">Ключ зарегистрированной привилегии.</param>
    /// <param name="duration">Положительный интервал продления.</param>
    /// <returns>
    /// <c>true</c>, если назначение было продлено; иначе <c>false</c>.
    /// </returns>
    Task<bool> ExtendAsync(
        ulong steamId,
        string privilegeKey,
        TimeSpan duration);

    /// <summary>
    /// Запрещает запуск новых отслеживаемых операций БД и синхронно
    /// ожидает завершения уже запущенных операций.
    /// </summary>
    /// <remarks>
    /// Вызывается при выгрузке плагина.
    /// </remarks>
    void StopAndWait();
}