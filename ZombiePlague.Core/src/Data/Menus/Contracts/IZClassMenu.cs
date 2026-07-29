using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.ZClasses;

namespace ZombiePlague.Core.Data.Menus.Contracts;

internal interface IZClassMenu
{
    void RegisterMenu();

    void Open(IPlayer player);

    IZClass GetPlayerZClass(IPlayer player);

    void RemovePlayer(int playerId);

    void Clear();
}
