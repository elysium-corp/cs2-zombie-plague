using Common.Di;
using Common.Di.Utils;
using Menu.Api;
using Menu.Api.Data.Contracts;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using ZombiePlague.Api;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Api;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Plugins.ResourceLoader;
using ZombiePlague.Core.Data.Zombies;
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
public sealed partial class ZombiePlague(ISwiftlyCore core) : Plugin<ZombiePlagueModule>(core)
{
    private readonly Lazy<IOptions<ZombiePlagueCoreConfig>> _config = GetRequiredServiceLazy<IOptions<ZombiePlagueCoreConfig>>();
    private readonly Lazy<IResourceLoader> _resourceLoader = GetRequiredServiceLazy<IResourceLoader>();
    private readonly Lazy<RoundManager> _roundManager = GetRequiredServiceLazy<RoundManager>();
    private readonly Lazy<IZombieManager> _zombieManager = GetRequiredServiceLazy<IZombieManager>();
    private readonly Lazy<IHumanManager> _humanManager = GetRequiredServiceLazy<IHumanManager>();
    private readonly Lazy<IKnockback> _knockback = GetRequiredServiceLazy<IKnockback>();
    private readonly Lazy<IEventSubscriber> _eventSubscriber = GetRequiredServiceLazy<IEventSubscriber>();

    private IMenuApi _menuApi = null!;
    
    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        var zombieManager = _zombieManager.Value;
        var humanManager = _humanManager.Value;
        var knockback = _knockback.Value;
        var zServiceApi = new ZombiePlagueApi(_eventSubscriber.Value, zombieManager, humanManager, knockback);
        interfaceManager.AddSharedInterface<IZombiePlagueApi, ZombiePlagueApi>(IZombiePlagueApi.SharedApiKey, zServiceApi);
    }

    public override void OnSharedInterfaceInjected(IInterfaceManager interfaceManager)
    {
        _menuApi = interfaceManager.GetSharedInterface<IMenuApi>(IMenuApi.SharedApiKey);
        _menuApi.EventSubscriber.OnMenuAddOption += OnMenuAddOption;
    }

    protected override void OnStart()
    {
        _resourceLoader.Value.Initialize();
        
        RegisterHooks();
        LoadFeatures();
    }

    private void OnMenuAddOption(Type menuType, DynamicOptionsMenu.MenuOptionsHolder holder)
    {
        var option1 = new ButtonMenuOption();
        option1.Text = "zpOption 1 [priority 1]";
        var option2 = new ButtonMenuOption();
        option2.Text = "zpOption 2 [priority 5]";
        
        holder.Add(option1, 1);
        holder.Add(option2, 5);
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
        var config = _config.GetOrNull();

        if (config == null)
        {
            return;
        }

        if (config.KnockbackEnabled)
        {
            _knockback.Value.Start();
        }
    }
}