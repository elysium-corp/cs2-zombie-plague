using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Data.Menus.Contracts;

internal interface IZClassMenu
{
    void RegisterMenu();

    void UnregisterMenu();

    void Open(IPlayer player);
}