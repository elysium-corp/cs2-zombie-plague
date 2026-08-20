using Admin.Core.Data;

namespace Admin.Core.Services;

/// <summary>
/// Предоставляет операции для работы с блокировками игроков.
/// </summary>
internal interface IBanService
{
    /// <summary>
    /// Создаёт новую блокировку игрока или обновляет уже существующую.
    /// </summary>
    /// <param name="steamId">
    /// SteamID64 блокируемого игрока.
    /// </param>
    /// <param name="bannedBySteamId">
    /// SteamID64 администратора, выдавшего блокировку.
    /// <c>null</c> допускается для системных или внешних источников блокировки.
    /// </param>
    /// <param name="duration">
    /// Продолжительность блокировки.
    /// <c>null</c> означает бессрочную блокировку.
    /// </param>
    /// <param name="reason">
    /// Причина блокировки.
    /// </param>
    /// <param name="cancellationToken">
    /// Токен отмены операции.
    /// </param>
    Task BanAsync(
        ulong steamId,
        ulong? bannedBySteamId,
        TimeSpan? duration,
        string reason,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Ищет активную блокировку игрока.
    /// </summary>
    /// <param name="steamId">
    /// SteamID64 игрока.
    /// </param>
    /// <param name="cancellationToken">
    /// Токен отмены операции.
    /// </param>
    /// <returns>
    /// Активную блокировку либо <c>null</c>, если игрок не заблокирован
    /// или срок его блокировки уже истёк.
    /// </returns>
    Task<ActiveBan?> FindActiveAsync(ulong steamId, CancellationToken cancellationToken = default);
}