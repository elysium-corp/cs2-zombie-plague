using Common.Database;
using Common.Database.Utils;
using Common.Di;
using Common.Di.Utils;
using CustomHud.Api;
using Localization.Api;
using MapRotation.Api;
using MapRotation.Core.Database;
using MapRotation.Core.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;

namespace MapRotation.Core;

[PluginMetadata(Id = "MapRotation.Core", Version = "1.3.0", Name = "Elysium Map Rotation", Author = "Elysium",
    Description = "Ротация карт, RTV, номинации и голосование через Custom HUD")]
internal sealed class MapRotationPlugin(ISwiftlyCore core) : Plugin<MapRotationModule>(core)
{
    private readonly Lazy<RotationCoordinator> _coordinator = GetRequiredServiceLazy<RotationCoordinator>();
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaces) =>
        interfaces.AddSharedInterface<IMapRotationApi, RotationEngine>(IMapRotationApi.SharedApiKey, GetRequiredService<RotationEngine>());
    protected override void OnUseSharedInterfaces(IInterfaceManager interfaces) =>
        BindSharedInterface<ILocalizationApi>(interfaces, ILocalizationApi.SharedApiKey);
    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaces)
    {
        BindSharedInterface<ILocalizationApi>(interfaces, ILocalizationApi.SharedApiKey);
        interfaces.TryGetSharedInterface<ICustomHudMenuApi>(ICustomHudMenuApi.SharedApiKey, out var menus);
        interfaces.TryGetSharedInterface<ICustomBannerApi>(ICustomBannerApi.SharedApiKey, out var banners);
        interfaces.TryGetSharedInterface<ICustomHudApi>(ICustomHudApi.SharedApiKey, out var messages);
        _coordinator.Value.Bind(menus, banners, messages);
    }
    protected override void OnReady() => _coordinator.Value.Start();
    protected override void OnUnload() { if (_coordinator.IsValueCreated) _coordinator.Value.Dispose(); }
}

internal sealed class MapRotationModule(ISwiftlyCore core) : BaseModule(core)
{
    public override (ServiceProvider, ServiceCollection) GetProvider()
    {
        var services = new ServiceCollection();
        services.AddSwiftly(Core);
        services.AddSharedInterface<ILocalizationApi>();
        services.AddSingleton<RotationText>();
        services.AddSingleton<IRotationHudPreferenceStore, RotationHudPreferenceStore>();
        services.AddSingleton<RotationHudPreferences>();
        services.AddPostgreSqlDatabase<MapRotationDbContext>(Core, new DatabaseOptions
        { ConnectionName = "map_rotation", Schema = MapRotationDbContext.SchemaName, CommandTimeoutSeconds = 5, RetryCount = 0 });
        services.AddSingleton<TimeProvider>(TimeProvider.System);
        services.AddSingleton<IRotationRandom, RotationRandom>();
        services.AddSingleton<RotationEngine>();
        services.AddSingleton<RotationStore>(provider => new(provider.GetRequiredService<IDbContextFactory<MapRotationDbContext>>(),
            provider.GetRequiredService<ILogger<RotationStore>>(), Core.PluginDataDirectory));
        services.AddSingleton<MapEngineAdapter>();
        services.AddSingleton<RotationCoordinator>();
        return (services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true, ValidateScopes = true }), services);
    }
}
