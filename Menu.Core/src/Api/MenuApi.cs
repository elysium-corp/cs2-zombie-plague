using Menu.Api;
using Menu.Api.Extensions;
using Menu.Api.Contracts;
using Menu.Api.Providers;
using Menu.Api.Results;
using Menu.Core.Application;
using Menu.Core.Providers;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Api;

internal sealed class MenuApi(
    IMenuExtensionRegistry extensionRegistry,
    IMenuExtensionDispatcher dispatcher,
    ProviderRegistry providers,
    MenuRuntimeService runtime) : IMenuApi
{
    public IMenuExtensionRegistry Extensions => extensionRegistry;

    public IMenuExtensionDispatcher Dispatcher => dispatcher;

    public IMenuProviderRegistration RegisterProvider(MenuProviderDescriptor descriptor) =>
        providers.Register(descriptor);

    public MenuOperationResult OpenMenu(IPlayer caller, string menuKey) =>
        runtime.OpenMenu(caller, menuKey);

    public MenuOperationResult OpenMenu(MenuOpenRequest request) =>
        runtime.OpenMenu(request);

    public MenuOperationResult OpenProviderMenu(
        IPlayer caller,
        string providerKey,
        string menuKey) => runtime.OpenProviderMenu(caller, providerKey, menuKey);

    public MenuCapabilityManifest GetCapabilities() => runtime.Capabilities;
}
