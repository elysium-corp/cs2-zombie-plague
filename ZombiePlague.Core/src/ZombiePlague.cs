using Admin.Api;
using Common.Database.Migrator;
using Common.Di;
using Common.Effects;
using Menu.Api;
using Menu.Api.Contracts;
using Menu.Api.Providers;
using Menu.Api.Results;
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
    private readonly Lazy<MainMenu> _mainMenu = GetRequiredServiceLazy<MainMenu>();
    private readonly Lazy<ZClassMenu> _zClassMenu = GetRequiredServiceLazy<ZClassMenu>();
    private readonly Lazy<DatabaseMigrator<ZombiePlagueDbContext>> _databaseMigrator = GetRequiredServiceLazy<DatabaseMigrator<ZombiePlagueDbContext>>();
    
    private readonly Lazy<AdminMenuExtension> _adminExtension = GetRequiredServiceLazy<AdminMenuExtension>();
    private IMenuProviderRegistration? _menuProviderRegistration;
    
    protected override void OnConfigureSharedInterfaces(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IZombiePlagueApi, ZombiePlagueApi>(
            IZombiePlagueApi.SharedApiKey,
            _api.Value
        );
    }

    protected override void OnSharedInterfacesInjected(IInterfaceManager interfaceManager)
    {
        var menuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);

        var adminApi = interfaceManager.GetSharedInterface<IAdminApi>(IAdminApi.SharedApiKey);

        _menuApiBridge.Value.Initialize(menuApi);
        RegisterMenuProvider(menuApi);
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
        _menuProviderRegistration?.Dispose();
        _menuProviderRegistration = null;
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

    private void RegisterMenuProvider(IMenuApi menuApi)
    {
        _menuProviderRegistration?.Dispose();
        _menuProviderRegistration = menuApi.RegisterProvider(new MenuProviderDescriptor
        {
            ProviderKey = "zombie_plague",
            DisplayName = "Zombie Plague",
            PluginVersion = BuildInfo.Version,
            Capabilities = [MenuProviderCapabilityKeys.OpenMenu],
        });

        if (!_menuProviderRegistration.IsRegistered)
        {
            return;
        }

        _menuProviderRegistration.RegisterMenu(CreateMenuDescriptor(
            "main",
            "Главное меню",
            "Main menu",
            context => _mainMenu.Value.Open(context.Target)));
        _menuProviderRegistration.RegisterMenu(CreateMenuDescriptor(
            "zclass",
            "Класс зомби",
            "Zombie class",
            context => _zClassMenu.Value.Open(context.Target)));
    }

    private static MenuProviderMenuDescriptor CreateMenuDescriptor(
        string key,
        string russianName,
        string englishName,
        Action<MenuProviderInvocationContext> open)
    {
        return new MenuProviderMenuDescriptor
        {
            MenuKey = key,
            DisplayName = new LocalizedText
            {
                Default = russianName,
                Translations = new Dictionary<string, string>
                {
                    ["ru"] = russianName,
                    ["en"] = englishName,
                },
            },
            Handler = context =>
            {
                open(context);
                return MenuOperationResult.Succeeded;
            },
        };
    }
}
