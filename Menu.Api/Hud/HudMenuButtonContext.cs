using SwiftlyS2.Shared.Players;

namespace Menu.Api.Hud;

/// <summary>
/// Содержит данные серверного обработчика нажатия кнопки HUD-меню.
/// </summary>
public sealed class HudMenuButtonContext(
    IPlayer player,
    string menuId,
    string buttonId,
    object? state,
    IHudMenuApi menu)
{
    /// <summary>
    /// Игрок, нажавший кнопку.
    /// </summary>
    public IPlayer Player { get; } = player;

    /// <summary>
    /// Идентификатор открытого меню.
    /// </summary>
    public string MenuId { get; } = menuId;

    /// <summary>
    /// Идентификатор кнопки из Panorama XML.
    /// </summary>
    public string ButtonId { get; } = buttonId;

    /// <summary>
    /// Пользовательское состояние, переданное через <see cref="HudMenuView.WithState"/>.
    /// </summary>
    public object? State { get; } = state;

    /// <summary>
    /// API меню, через которое обработчик может обновить или закрыть представление.
    /// </summary>
    public IHudMenuApi Menu { get; } = menu;
}
