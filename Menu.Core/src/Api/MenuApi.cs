using Menu.Api;
using Menu.Api.Data.Contracts;
using Menu.Api.Events;
using Menu.Core.Service;

namespace Menu.Core.Api;

internal sealed class MenuApi(IMenuService menuService, IEventSubscriber eventSubscriber) : IMenuApi
{
    public IEventSubscriber EventSubscriber => eventSubscriber;

    public TMenu CreateMenu<TMenu>() where TMenu : class, IMenu => menuService.CreateMenu<TMenu>();
}