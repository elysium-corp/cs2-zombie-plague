using Common.Menu.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Common.Menu;

public class MenuService(ISwiftlyCore core) : IMenuService
{
    void Initialize()
    {
        
    }

    public void OpenMenu(IPlayer player)
    {
        var menu = new MainMenu(core);
        
        menu.Open(player);
    }
}