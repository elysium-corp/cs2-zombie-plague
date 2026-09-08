using Common.Di;
using CustomHud.Api;
using Localization.Api;
using Microsoft.Extensions.DependencyInjection;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace CustomHud.Core;

[PluginMetadata(Id = "CustomHud.Core", Version = "1.1.1", Name = "Elysium Custom HUD",
    Author = "Elysium", Description = "Общий API цветных HUD-сообщений и баннеров")]
internal sealed class CustomHudPlugin(ISwiftlyCore core) : Plugin<CustomHudModule>(core)
{
    private readonly Lazy<CustomHudService> _service = GetRequiredServiceLazy<CustomHudService>();

    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaces)
    {
        interfaces.AddSharedInterface<ICustomHudApi, CustomHudService>(ICustomHudApi.SharedApiKey, _service.Value);
        interfaces.AddSharedInterface<ICustomBannerApi, CustomHudService>(ICustomBannerApi.SharedApiKey, _service.Value);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaces)
    {
        interfaces.TryGetSharedInterface<ILocalizationApi>(ILocalizationApi.SharedApiKey, out var localization);
        _service.Value.InitializeLocalization(localization);
    }

    protected override void OnReady() => _service.Value.Start();

    protected override void OnUnload()
    {
        if (_service.IsValueCreated) _service.Value.Dispose();
    }
}

internal sealed class CustomHudModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();
        services.AddSwiftly(Core);
        AddConfig<CustomHudConfig>(services, "custom_hud.json", "CustomHud", reloadOnChange: false);
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<Func<IHudRuntime>>(_ => () => new PanoramaHudRuntime(Core));
        services.AddSingleton<CustomHudService>();
        return (services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true, ValidateScopes = true
        }), services);
    }
}

internal sealed class CustomHudConfig
{
    public bool Enabled { get; set; } = true;
}
