using Menu.Api.Data.Contracts;

namespace Menu.Core.Service;

internal interface IMenuService
{
    TMenu CreateMenu<TMenu>() where TMenu : class, IMenu;
}