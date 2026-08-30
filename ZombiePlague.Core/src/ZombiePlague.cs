using Admin.Api;
using Common.Database.Migrator;
using Common.Di;
using Common.Effects;
using Localization.Api;
using Menu.Api;
using Metrics.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using ZombiePlague.Api;
using ZombiePlague.Core.Api;
using ZombiePlague.Core.Data.Coordinators.Contracts;
using ZombiePlague.Core.Data.Plugins.ResourceLoader;
using ZombiePlague.Core.Database;
using ZombiePlague.Core.Di;
using ZombiePlague.Core.Generated;
using ZombiePlague.Core.Menus;
using ZombiePlague.Core.Menus.Admin;
using ZombiePlague.Core.Metrics;

namespace ZombiePlague.Core;

[PluginMetadata(
    Id = "ZombiePlague.Core",
    Version = BuildInfo.Version,
    Name = "ZombiePlague.Core",
    Author = "illusion & fdrinv",
    Description = "Zombie Plague Core for CS2"
)]
public sealed partial class ZombiePlague(ISwiftlyCore core) : Plugin<ZombiePlagueModule>(core)
{
    private readonly Lazy<IResourceLoader> _resourceLoader = GetRequiredServiceLazy<IResourceLoader>();
    private readonly Lazy<IZombiePlagueCoordinator> _coordinator = GetRequiredServiceLazy<IZombiePlagueCoordinator>();
    private readonly Lazy<ZombiePlagueApi> _api = GetRequiredServiceLazy<ZombiePlagueApi>();
    private readonly Lazy<MenuExtensionDispatcherProxy> _menuApiBridge = GetRequiredServiceLazy<MenuExtensionDispatcherProxy>();
    private readonly Lazy<AdminApiProxy> _adminApiBridge = GetRequiredServiceLazy<AdminApiProxy>();
    private readonly Lazy<MetricsServiceProxy> _metricsApiBridge = GetRequiredServiceLazy<MetricsServiceProxy>();
    private readonly Lazy<DatabaseMigrator<ZombiePlagueDbContext>> _databaseMigrator = GetRequiredServiceLazy<DatabaseMigrator<ZombiePlagueDbContext>>();
    
    private readonly Lazy<AdminMenuExtension> _adminExtension = GetRequiredServiceLazy<AdminMenuExtension>();
    
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IZombiePlagueApi, ZombiePlagueApi>(
            IZombiePlagueApi.SharedApiKey,
            _api.Value
        );
    }

    protected override void OnUseSharedInterfaces(IInterfaceManager interfaceManager)
    {
        BindSharedInterface<ILocalizationApi>(interfaceManager, ILocalizationApi.SharedApiKey);
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        var menuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);

        var adminApi = interfaceManager.GetSharedInterface<IAdminApi>(IAdminApi.SharedApiKey);

        _menuApiBridge.Value.Initialize(menuApi);
        _adminApiBridge.Value.Initialize(adminApi);

        if (interfaceManager.TryGetSharedInterface<IMetricsService>(IMetricsService.SharedApiKey, out var metricsApi))
        {
            _metricsApiBridge.Value.Initialize(metricsApi);
        }
        else
        {
            Core.Logger.LogWarning(
                "Metrics.Core is not loaded. ZombiePlague.Core will continue without analytics events."
            );
        }

        _adminExtension.Value.Initialize(menuApi);
    }

    protected override void OnStart()
    {
        TryMigrateDatabase();
        
        _resourceLoader.Value.Initialize();
        _coordinator.Value.Start();
    }

    protected override void OnUnload()
    {
        EffectService.Release(Core);
        _adminExtension.Value.Uninitialize();
        _metricsApiBridge.Value.Uninitialize();
        _adminApiBridge.Value.Uninitialize();

        _coordinator.Value.Stop();
        _resourceLoader.Value.Uninitialize();
    }

    private void TryMigrateDatabase()
    {
        try
        {
            _databaseMigrator.Value.Migrate();
        }
        catch (Exception exception)
        {
            core.Logger.LogError(
                exception,
                "Zombie Plague database migration failed. Default player preferences will be used."
            );
        }
    }
}
