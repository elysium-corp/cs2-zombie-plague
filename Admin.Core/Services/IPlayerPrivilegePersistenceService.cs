using Admin.Core.Data;

namespace Admin.Core.Services;

/// <summary>
/// Загружает назначения привилегий игроков
/// из persistent-хранилища.
/// </summary>
internal interface IPlayerPrivilegePersistenceService
{
    /// <summary>
    /// Загружает активные назначения игрока.
    /// </summary>
    /// <remarks>
    /// Истёкшие назначения отбрасываются непосредственно SQL-запросом.
    /// Бессрочные назначения считаются активными.
    /// </remarks>
    Task<IReadOnlyCollection<PlayerPrivilege>> LoadAsync(
        ulong steamId,
        CancellationToken cancellationToken = default);
}