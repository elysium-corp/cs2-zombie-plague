using SwiftlyS2.Shared.Players;
using ZPCore.Data.Zombies.ZClasses;

namespace ZPCore.Data.Menus.Contracts;

public interface IZClassMenu
{
    public void RegisterMenu();
    public void Open(IPlayer player);
    public IZClass GetPlayerZClass(IPlayer player);
}