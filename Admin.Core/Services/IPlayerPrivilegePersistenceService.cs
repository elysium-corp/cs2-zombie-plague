using Admin.Core.Data;

namespace Admin.Core.Services;

/// <summary>
/// Предоставляет низкоуровневые операции persistent-хранилища
/// назначений привилегий игроков.
/// </summary>
/// <remarks>
/// Сервис отвечает только за данные в базе и не управляет runtime-хранилищем игроков.
/// </remarks>
internal interface IPlayerPrivilegePersistenceService
{
    /// <summary>
    /// Продлевает существующее временное назначение.
    /// </summary>
    /// <remarks>
    /// Активное назначение продлевается от текущего срока окончания,
    /// истёкшее — от текущего UTC-времени.
    ///
    /// Для бессрочного или отсутствующего назначения возвращается <c>null</c>.
    /// </remarks>
    Task<PlayerPrivilege?> ExtendAsync(
        ulong steamId,
        string privilegeKey,
        TimeSpan duration,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Ищет сохранённое назначение независимо от срока его действия.
    /// </summary>
    /// <returns>
    /// Назначение, включая уже истёкшее, либо <c>null</c>.
    /// </returns>
    Task<PlayerPrivilege?> FindAsync(
        ulong steamId,
        string privilegeKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Загружает только активные назначения игрока.
    /// </summary>
    /// <remarks>
    /// Истёкшие назначения исключаются непосредственно SQL-запросом.
    /// Бессрочные назначения считаются активными.
    /// </remarks>
    Task<IReadOnlyCollection<PlayerPrivilege>> LoadAsync(
        ulong steamId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Создаёт новое назначение либо обновляет срок существующего.
    /// </summary>
    /// <remarks>
    /// При обновлении существующей записи <c>CreatedAtUtc</c> сохраняется,
    /// а <c>UpdatedAtUtc</c> обновляется.
    /// </remarks>
    Task<PlayerPrivilege> UpsertAsync(
        ulong steamId,
        string privilegeKey,
        DateTime? expiresAtUtc,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Физически удаляет назначение из persistent-хранилища.
    /// </summary>
    /// <returns>
    /// <c>true</c>, если строка существовала и была удалена.
    /// </returns>
    Task<bool> DeleteAsync(
        ulong steamId,
        string privilegeKey,
        CancellationToken cancellationToken = default);
}