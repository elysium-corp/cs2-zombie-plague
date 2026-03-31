using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Plugins;
using ZPApi;
using ZPApi.Events;
using ZPCore.Api;
using ZPCore.Config.Core;
using ZPCore.Data;
using ZPCore.Data.Lifecycle;
using ZPCore.Data.Managers;
using ZPCore.Data.Menus;
using ZPCore.Data.Plugins.AdminMenu;
using ZPCore.Data.Plugins.ResourceLoader;
using ZPCore.Data.Weapons;
using ZPCore.Di;
using ZPCore.Generated;

namespace ZPCore;

[PluginMetadata(
    Id = "ZPCore",
    Version = BuildInfo.Version,
    Name = "ZPCore",
    Author = "illusion & fdrinv",
    Description = "Zombie Plague mode for CS2"
)]
public sealed partial class ZPCore(ISwiftlyCore core) : BasePlugin(core)
{
    private readonly Lazy<IResourceLoader> _resourceLoader = new(DependencyManager.GetService<IResourceLoader>);
    private readonly Lazy<RoundManager> _roundManager = new(DependencyManager.GetService<RoundManager>);
    private readonly Lazy<ZombieManager> _zombieManager = new(DependencyManager.GetService<ZombieManager>);
    private readonly Lazy<HumanManager> _humanManager = new(DependencyManager.GetService<HumanManager>);
    private readonly Lazy<Knockback> _knockback = new(DependencyManager.GetService<Knockback>);
    private readonly Lazy<EffectManager> _effectManager = new(DependencyManager.GetService<EffectManager>);
    private readonly Lazy<LifecycleManager> _lifecycleManager = new(DependencyManager.GetService<LifecycleManager>);

    private readonly Lazy<IWeaponRegistrator>
        _weaponRegistrator = new(DependencyManager.GetService<IWeaponRegistrator>);

    public override void ConfigureSharedInterface(IInterfaceManager interfaceManager)
    {
        var eventSubscriber = DependencyManager.GetService<IEventSubscriber>();
        var zServiceApi = new ZServiceApi(eventSubscriber);
        interfaceManager.AddSharedInterface<IZServiceApi, ZServiceApi>(IZServiceApi.SharedApiKey, zServiceApi);
    }

    public override void Load(bool hotReload)
    {
        if (hotReload)
        {
            DependencyManager.Dispose();
            _lifecycleManager.Value.Dispose();
        }

        DependencyManager.Load(Core);

        _resourceLoader.Value.Initialize();

        _weaponRegistrator.Value.Registration();
        _lifecycleManager.Value.Initialize();

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
        _effectManager.Value.RegisterHooks();
        _roundManager.Value.RegisterHooks();
    }

    private void LoadFeatures()
    {
        var config = DependencyManager.GetService<IOptions<ZombiePlagueCoreConfig>>().Value;

        if (config.KnockbackEnabled)
        {
            _knockback.Value.Start();
        }
        
        RegisterCommands();
    }

    private void RegisterCommands()
    {
        Core.Command.RegisterCommand(
            commandName: "gun",
            handler: GunHandler,
            registerRaw: true
        );
    }

    private void GunHandler(ICommandContext context)
    {
        var player = context.Sender;

        if (!context.IsSentByPlayer)
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        var weaponBuyMenu = new WeaponCategoriesMenu(core);
        weaponBuyMenu.Open(player);
    }
}