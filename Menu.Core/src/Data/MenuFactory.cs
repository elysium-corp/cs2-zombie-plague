using Menu.Api.Data.Contracts;
using Menu.Api.Events;
using Menu.Core.Data.Menus;
using SwiftlyS2.Shared;

namespace Menu.Core.Data;

internal class MenuFactory(ISwiftlyCore core, IEventPublisher eventPublisher) : IMenuFactory
{
    public TMenu? Create<TMenu>() where TMenu : class, IMenu
    {
        return typeof(TMenu) switch
        {
            var t when t == typeof(Main) => new Main(core, eventPublisher) as TMenu,
            _ => throw new NotSupportedException("MenuFactory: type TMenu hasn't supported!")
        };
    }
}