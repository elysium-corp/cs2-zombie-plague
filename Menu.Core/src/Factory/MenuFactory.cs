using Menu.Api.Data.Contracts;
using Menu.Api.Data.Factory;
using Menu.Api.Data.Menus;
using Menu.Api.Events;
using Menu.Core.Menus;
using SwiftlyS2.Shared;

namespace Menu.Core.Factory;

internal class MenuFactory(ISwiftlyCore core, IEventPublisher eventPublisher) : IMenuFactory
{
    public TMenu? Create<TMenu>() where TMenu : class, IMenu
    {
        return typeof(TMenu) switch
        {
            var t when t == typeof(IMainMenu) => new MainMenu(core, eventPublisher) as TMenu,
            var t when t == typeof(IZClassMenu) => new ZClassMenu(core, eventPublisher) as TMenu,
            _ => throw new NotSupportedException("MenuFactory: type TMenu hasn't supported!")
        };
    }
}