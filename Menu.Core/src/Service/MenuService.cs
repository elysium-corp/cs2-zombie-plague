using Menu.Api.Data.Contracts;
using Menu.Api.Data.Factory;

namespace Menu.Core.Service;

internal sealed class MenuService(IMenuFactory menuFactory) : IMenuService
{
    public TMenu CreateMenu<TMenu>() where TMenu : class, IMenu
    {
        return menuFactory.Create<TMenu>() ?? throw new NullReferenceException("Menu factory returned null");
    }
}