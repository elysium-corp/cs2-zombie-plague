using Menu.Api;
using Menu.Api.Extensions;
using Menu.Api.Hud;

namespace Menu.Core.Api;

internal sealed class MenuApi(
    IMenuExtensionRegistry extensionRegistry,
    IMenuExtensionDispatcher dispatcher,
    IHudMenuApi hudMenu
) : IMenuApi
{
    public IMenuExtensionRegistry Extensions => extensionRegistry;

    public IMenuExtensionDispatcher Dispatcher => dispatcher;

    public IHudMenuApi Hud => hudMenu;
}
