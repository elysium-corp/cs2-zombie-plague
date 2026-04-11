using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Zombies.ZClasses;

namespace ZombiePlague.Core.Data.Menus.Contracts;

public interface IZClassMenu
{
    public void RegisterMenu();
    public void Open(IPlayer player);
    public IZClass GetPlayerZClass(IPlayer player);
}