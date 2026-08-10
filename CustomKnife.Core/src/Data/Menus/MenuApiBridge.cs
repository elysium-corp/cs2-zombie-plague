using Menu.Api;
using Menu.Api.Extensions;

namespace CustomKnife.Data.Menus;

internal sealed class MenuApiBridge : IMenuExtensionDispatcher
{
    private IMenuApi? _menuApi;

    public IMenuExtensionRegistry Extensions => MenuApi.Extensions;

    private IMenuApi MenuApi => _menuApi ?? throw new InvalidOperationException("Menu API is not initialized!");

    public void Initialize(IMenuApi menuApi)
    {
        ArgumentNullException.ThrowIfNull(menuApi);

        _menuApi = menuApi;
    }

    public void Dispatch(string menuId, MenuExtensionContext context)
    {
        MenuApi.Dispatcher.Dispatch(menuId, context);
    }
}