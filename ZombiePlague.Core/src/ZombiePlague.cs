using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Plugins;
using ZombiePlague.Api;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Api;
using ZombiePlague.Core.Data;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Plugins.AdminMenu;
using ZombiePlague.Core.Data.Plugins.ResourceLoader;
using ZombiePlague.Core.Di;
using ZombiePlague.Core.Generated;
using ZPCore.Config.Core;

namespace ZombiePlague.Core;

[PluginMetadata(
    Id = "ZombiePlague.Core",
    Version = BuildInfo.Version,
    Name = "ZombiePlague.Core",
    Author = "illusion & fdrinv",
    Description = "Zombie Plague Core for CS2"
)]
public sealed partial class ZombiePlague(ISwiftlyCore core) : BasePlugin(core)
{
    private readonly Lazy<IResourceLoader> _resourceLoader = new(DependencyManager.GetService<IResourceLoader>);
    private readonly Lazy<RoundManager> _roundManager = new(DependencyManager.GetService<RoundManager>);
    private readonly Lazy<ZombieManager> _zombieManager = new(DependencyManager.GetService<ZombieManager>);
    private readonly Lazy<HumanManager> _humanManager = new(DependencyManager.GetService<HumanManager>);
    private readonly Lazy<Knockback> _knockback = new(DependencyManager.GetService<Knockback>);

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        var eventSubscriber = DependencyManager.GetService<IEventSubscriber>();
        var zServiceApi = new ZombiePlagueApi(eventSubscriber);
        interfaceManager.AddSharedInterface<IZombiePlagueApi, ZombiePlagueApi>(IZombiePlagueApi.SharedApiKey, zServiceApi);
    }

    public override void Load(bool hotReload)
    {
        if (hotReload)
        {
            DependencyManager.Dispose();
        }

        DependencyManager.Load(Core);

        _resourceLoader.Value.Initialize();
        
        RegisterHooks();
        LoadFeatures();

        new AdminMenu(Core, _roundManager.Value, _zombieManager.Value).Load();
    }

    public override void Unload()
    {
    }

    private void RegisterHooks()
    {
        _roundManager.Value.RegisterRounds();
        _zombieManager.Value.RegisterHooks();
        _humanManager.Value.RegisterHooks();
        _roundManager.Value.RegisterHooks();
    }

    private void LoadFeatures()
    {
        var config = DependencyManager.GetService<IOptions<ZombiePlagueCoreConfig>>().Value;

        if (config.KnockbackEnabled)
        {
            _knockback.Value.Start();
        }
    }
}