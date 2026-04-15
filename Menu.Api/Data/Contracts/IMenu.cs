using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Data.Contracts;

public interface IMenu
{
    void Open(IPlayer player);
    
    void OpenAll(Predicate<IPlayer>? predicate);

    void Close(IPlayer player);

    void CloseAll();

    void CloseBy(Predicate<IPlayer>? predicate);
    
    IMenuBuilderAPI Builder(IPlayer player);
}