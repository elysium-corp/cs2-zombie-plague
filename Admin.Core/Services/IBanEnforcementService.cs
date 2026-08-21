using SwiftlyS2.Shared.Players;

namespace Admin.Core.Services;

/// <summary>
/// Применяет активные блокировки к подключённым игрокам.
/// </summary>
internal interface IBanEnforcementService
{
    /// <summary>
    /// Запускает проверку активной блокировки игрока.
    /// </summary>
    /// <param name="player">
    /// Авторизованный игрок.
    /// </param>
    void Check(IPlayer player);
}