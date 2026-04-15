using SwiftlyS2.Shared.Players;

namespace Common.Menu.Data.Contracts;

public interface IMenu
{
    void Open(IPlayer player);

    void OpenAll();

    void OpenBy(Predicate<IPlayer>? predicate);

    void Close(IPlayer player);

    void CloseAll();

    void CloseBy(Predicate<IPlayer>? predicate);
}