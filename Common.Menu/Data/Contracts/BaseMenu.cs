using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Common.Menu.Data.Contracts; 

public abstract class BaseMenu(ISwiftlyCore core) : IMenu
{
    private readonly IMenuManagerAPI _menuManager = core.MenusAPI;

    public IMenuAPI? Menu { get; private set; }

    private IMenuAPI GetOrCreateMenu()
    {
        Menu ??= Builder(_menuManager).Build();
        return Menu;
    }

    public virtual void Open(IPlayer player)
    {
        var menu = GetOrCreateMenu();
        _menuManager.OpenMenuForPlayer(player, menu);
    }

    public virtual void OpenAll()
    {
        var menu = GetOrCreateMenu();
        _menuManager.OpenMenu(menu);
    }
    
    public virtual void OpenBy(Predicate<IPlayer>? predicate)
    {
        if (Menu is null) return;

        var players = core.PlayerManager.GetAllValidPlayers();

        foreach (var player in players)
        {
            var condition = predicate?.Invoke(player) ?? true;
            if (condition)
            {
                Open(player);
            }
        }
    }

    public virtual void Close(IPlayer player)
    {
        if (Menu is null) return;

        _menuManager.CloseMenuForPlayer(player, Menu);
    }

    public virtual void CloseAll()
    {
        if (Menu is null) return;

        _menuManager.CloseMenu(Menu);
    }
    
    public virtual void CloseBy(Predicate<IPlayer>? predicate)
    {
        if (Menu is null) return;

        var players = core.PlayerManager.GetAllValidPlayers();

        foreach (var player in players)
        {
            var condition = predicate?.Invoke(player) ?? true;
            if (condition)
            {
                Close(player);
            }
        }
    }

    public abstract IMenuBuilderAPI Builder(IMenuManagerAPI manager, IMenuAPI? parent = null);
}