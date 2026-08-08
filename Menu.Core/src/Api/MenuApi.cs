using Menu.Api;
using Menu.Api.Extensions;

namespace Menu.Core.Api;

internal sealed class MenuApi(
    IMenuExtensionRegistry extensionRegistry,
    IMenuExtensionDispatcher dispatcher
) : IMenuApi
{
    public IMenuExtensionRegistry Extensions => extensionRegistry;

    public IMenuExtensionDispatcher Dispatcher => dispatcher;
}