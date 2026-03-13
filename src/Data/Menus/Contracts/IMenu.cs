using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Menus.Contracts;

public interface IMenu
{
    public IMenuAPI Open(IPlayer player, IMenuAPI? parent = null);

    public void OpenAll(Predicate<IPlayer>? predicate, IMenuAPI? parent = null);

    public IMenuBuilderAPI Builder(IPlayer player, IMenuAPI? parent = null);
}