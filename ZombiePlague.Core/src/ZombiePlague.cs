using Common.Di;
using Menu.Api;
using SwiftlyS2.Shared;
using ZombiePlague.Api;
using ZombiePlague.Core.Api;
using ZombiePlague.Core.Data;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Plugins.AdminMenu;
using ZombiePlague.Core.Data.Plugins.ResourceLoader;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Rounds.Registrator;
using ZombiePlague.Core.Di;
using ZombiePlague.Core.Generated;
using ZombiePlague.Core.Menus.Factories;

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
    private readonly Lazy<ICoreCoordinator> _coordinator = GetRequiredServiceLazy<ICoreCoordinator>();
    private readonly Lazy<ZombiePlagueApi> _api = GetRequiredServiceLazy<ZombiePlagueApi>();
    private readonly Lazy<IMainMenuItemFactory> _mainMenuItemFactory = GetRequiredServiceLazy<IMainMenuItemFactory>();
    private readonly Lazy<IZClassMenuItemFactory> _zClassMenuItemFactory = GetRequiredServiceLazy<IZClassMenuItemFactory>();

    public static IMenuApi MenuApi = null!;
    
    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        interfaceManager.AddSharedInterface<IZombiePlagueApi, ZombiePlagueApi>(IZombiePlagueApi.SharedApiKey, _api.Value);
    }
    
    public override void OnSharedInterfaceInjected(IInterfaceManager interfaceManager)
    {
        MenuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);
        
        MenuApi.EventSubscriber.OnMainMenuAddOption += _mainMenuItemFactory.Value.OnMainMenuAddOption;
        MenuApi.EventSubscriber.OnZClassMenuAddOption += _zClassMenuItemFactory.Value.OnZClassMenuAddOption;
    }

    protected override void OnStart()
    {
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
}
