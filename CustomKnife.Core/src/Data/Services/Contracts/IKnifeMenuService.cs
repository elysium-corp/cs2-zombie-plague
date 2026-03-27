using SwiftlyS2.Shared.Players;

namespace CustomKnife.Data.Services.Contracts;

public interface IKnifeMenuService
{
    /// <summary>
    /// Показывает меню конкретному игроку.
    /// </summary>
    public void Show(IPlayer player);
}