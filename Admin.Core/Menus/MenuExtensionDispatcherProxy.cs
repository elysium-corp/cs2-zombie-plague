using Menu.Api;
using Menu.Api.Extensions;

namespace Admin.Core.Menus;

internal sealed class MenuExtensionDispatcherProxy : IMenuExtensionDispatcher
{
    private IMenuExtensionDispatcher? _dispatcher;

    public void Initialize(IMenuApi menuApi)
    {
        _dispatcher = menuApi.Dispatcher;
    }

    public void Dispatch(string menuId, MenuExtensionContext context)
    {
        var dispatcher = _dispatcher ?? throw new InvalidOperationException("Menu API is not initialized!");

        dispatcher.Dispatch(menuId, context);
    }
}