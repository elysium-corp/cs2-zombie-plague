using Common.Di;
using Menu.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using ZombiePlague.Api;
using ZombiePlague.Core.Api;
using ZombiePlague.Core.Data;
using ZombiePlague.Core.Data.Coordinators;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Plugins.AdminMenu;
using ZombiePlague.Core.Data.Plugins.ResourceLoader;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Rounds.Registrator;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Di;
using ZombiePlague.Core.Generated;
using ZombiePlague.Core.Menus;

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
    private readonly Lazy<IPlayerPersistenceService> _playerPersistenceService = GetRequiredServiceLazy<IPlayerPersistenceService>();

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

        _menuApiBridge.Value.Initialize(menuApi);
    }

    protected override void OnStart()
    {
        TryInitializeDatabase();
        _resourceLoader.Value.Initialize();
        _coordinator.Value.Start();

        var playerManager = DependencyResolver.GetRequiredService<IPlayerManager>();
        var roundManager = DependencyResolver.GetRequiredService<IRoundManager>();
        var roundFactory = DependencyResolver.GetRequiredService<IRoundFactory>();
        var roundRegistry = DependencyResolver.GetRequiredService<IRoundRegistrator>();
        var adminMenu = new AdminMenu(core, playerManager, roundManager, roundRegistry, roundFactory);

        adminMenu.Load();
    }

    protected override void OnUnload()
    {
        _coordinator.Value.Stop();
        _resourceLoader.Value.Uninitialize();
    }

    private void TryInitializeDatabase()
    {
        try
        {
            _playerPersistenceService.Value.InitializeDatabase();
        }
        catch (Exception exception)
        {
            core.Logger.LogError(
                exception,
                "Zombie Plague database initialization failed. Default player preferences will be used."
            );
        }
    }
}
