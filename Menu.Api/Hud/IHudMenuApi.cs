using SwiftlyS2.Shared.Players;

namespace Menu.Api.Hud;

/// <summary>
/// Управляет интерактивными меню, отрисованными сущностью <c>custom_hud_layout</c>.
/// </summary>
/// <remarks>
/// Все методы, изменяющие состояние меню, должны вызываться из игрового потока SwiftlyS2.
/// Одновременно у игрока может быть открыто только одно HUD-меню.
/// </remarks>
public interface IHudMenuApi
{
    /// <summary>
    /// Регистрирует описание HUD-меню и его обработчики кнопок.
    /// </summary>
    /// <param name="definition">Описание меню.</param>
    /// <returns>Подписка, удаляющая меню и освобождающая связанные ресурсы при вызове <see cref="IDisposable.Dispose"/>.</returns>
    IDisposable Register(HudMenuDefinition definition);

    /// <summary>
    /// Открывает меню игроку, применяет персональные данные и захватывает ввод мыши.
    /// </summary>
    /// <param name="player">Игрок, которому открывается меню.</param>
    /// <param name="menuId">Идентификатор зарегистрированного меню.</param>
    /// <param name="view">Персональное состояние представления.</param>
    void Open(IPlayer player, string menuId, HudMenuView view);

    /// <summary>
    /// Обновляет уже открытое меню без пересоздания сущности и повторного захвата ввода.
    /// </summary>
    /// <param name="player">Игрок с открытым меню.</param>
    /// <param name="menuId">Идентификатор меню.</param>
    /// <param name="view">Новое персональное состояние представления.</param>
    /// <returns><see langword="true"/>, если меню было открыто и обновлено.</returns>
    bool Update(IPlayer player, string menuId, HudMenuView view);

    /// <summary>
    /// Закрывает активное HUD-меню игрока и освобождает захваченный ввод.
    /// </summary>
    /// <param name="player">Игрок, меню которого требуется закрыть.</param>
    void Close(IPlayer player);

    /// <summary>
    /// Проверяет, открыто ли указанное HUD-меню у игрока.
    /// </summary>
    /// <param name="player">Проверяемый игрок.</param>
    /// <param name="menuId">Идентификатор меню.</param>
    /// <returns><see langword="true"/>, если это меню сейчас активно.</returns>
    bool IsOpen(IPlayer player, string menuId);
}
