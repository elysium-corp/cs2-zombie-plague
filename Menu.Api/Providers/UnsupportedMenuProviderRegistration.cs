using Menu.Api.Enums;
using Menu.Api.Results;

namespace Menu.Api.Providers;

internal sealed class UnsupportedMenuProviderRegistration : IMenuProviderRegistration
{
    private static readonly MenuOperationResult UnsupportedResult = MenuOperationResult.Unsupported(
        "provider_api_not_implemented"
    );

    private UnsupportedMenuProviderRegistration(string providerKey)
    {
        ProviderKey = providerKey;
    }

    public string ProviderKey { get; }

    public bool IsRegistered => false;

    public MenuOperationResult RegistrationResult => UnsupportedResult;

    public static IMenuProviderRegistration Create(string? providerKey)
    {
        return new UnsupportedMenuProviderRegistration(providerKey ?? string.Empty);
    }

    public MenuOperationResult RegisterMenu(MenuProviderMenuDescriptor descriptor)
    {
        return UnsupportedResult;
    }

    public MenuOperationResult RegisterAction(MenuProviderActionDescriptor descriptor)
    {
        return UnsupportedResult;
    }

    public MenuOperationResult UnregisterMenu(string menuKey)
    {
        return UnsupportedResult;
    }

    public MenuOperationResult UnregisterAction(string actionKey)
    {
        return UnsupportedResult;
    }

    public MenuOperationResult UnregisterProvider()
    {
        return MenuOperationResult.Failure(MenuOperationStatus.Disposed, "provider_not_registered");
    }

    public void Dispose()
    {
    }
}
