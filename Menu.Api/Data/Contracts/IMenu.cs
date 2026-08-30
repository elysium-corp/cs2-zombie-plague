using SwiftlyS2.Shared.Players;

namespace Menu.Api.Data.Contracts;

/// <summary>
/// Представляет устаревшее программно построенное меню SwiftlyS2.
/// </summary>
public interface IMenu
{
    /// <summary>
    /// Возвращает стабильный технический идентификатор меню.
    /// </summary>
    string Id { get; }

    /// <summary>
    /// Открывает меню указанному валидному игроку, если он имеет доступ.
    /// </summary>
    /// <param name="player">Получатель меню.</param>
    void Open(IPlayer player);
}
