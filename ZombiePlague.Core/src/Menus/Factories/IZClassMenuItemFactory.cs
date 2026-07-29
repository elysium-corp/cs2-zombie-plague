using Menu.Api.Data.Contracts;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Menus.Factories;

internal interface IZClassMenuItemFactory
{
    public void OnZClassMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder);
}