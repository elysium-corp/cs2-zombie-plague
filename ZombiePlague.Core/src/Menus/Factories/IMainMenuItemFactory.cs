using Menu.Api.Data.Contracts;
using SwiftlyS2.Shared.Players;

namespace ZombiePlague.Core.Menus.Factories;

internal interface IMainMenuItemFactory
{
    void OnMainMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder);
}