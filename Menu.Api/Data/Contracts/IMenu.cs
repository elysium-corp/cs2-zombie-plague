using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Menu.Api.Data.Contracts;

public interface IMenu
{
    string Id { get; }
    
    void Open(IPlayer player);
}