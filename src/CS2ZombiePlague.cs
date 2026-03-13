using CS2ZombiePlague.Config;
using CS2ZombiePlague.Config.InfoNotify;
using CS2ZombiePlague.Config.models;
using CS2ZombiePlague.Data;
using CS2ZombiePlague.Data.Lifecycle;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Menus;
using CS2ZombiePlague.Data.Plugins.AdminMenu;
using CS2ZombiePlague.Data.Plugins.DamageNotify;
using CS2ZombiePlague.Data.Plugins.InfoNotify;
using CS2ZombiePlague.Data.Plugins.ModelChanger;
using CS2ZombiePlague.Data.Plugins.MoneySystem;
using CS2ZombiePlague.Data.Plugins.ResetScore;
using CS2ZombiePlague.Data.Plugins.ResourceLoader;
using CS2ZombiePlague.Data.Plugins.RoundRatingNotify;
using CS2ZombiePlague.Data.Plugins.ScreenFade;
using CS2ZombiePlague.Data.Plugins.SupplyBox;
using CS2ZombiePlague.Data.Weapons;
using CS2ZombiePlague.Di;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Plugins;

namespace CS2ZombiePlague
{
    [PluginMetadata(Id = "CS2ZombiePlague", Version = "1.0.0", Name = "CS2ZombiePlague", Author = "illusion & fdrinv",
        Description = "Zombie Plague mode for CS2")]
    public partial class CS2ZombiePlague(ISwiftlyCore core) : BasePlugin(core)
    {
        private readonly Lazy<IResourceLoader> _resourceLoader = new(DependencyManager.GetService<IResourceLoader>);
        private readonly Lazy<RoundManager> _roundManager = new(DependencyManager.GetService<RoundManager>);
        private readonly Lazy<ZombieManager> _zombieManager = new(DependencyManager.GetService<ZombieManager>);
        private readonly Lazy<HumanManager> _humanManager = new(DependencyManager.GetService<HumanManager>);
        private readonly Lazy<KnifeManager> _knifeManager = new(DependencyManager.GetService<KnifeManager>);
        private readonly Lazy<Knockback> _knockback = new(DependencyManager.GetService<Knockback>);
        private readonly Lazy<DamageNotify> _damageNotify = new(DependencyManager.GetService<DamageNotify>);
        private readonly Lazy<MoneySystem> _moneySystem = new(DependencyManager.GetService<MoneySystem>);
        private readonly Lazy<ScreenFade> _screenFade = new(DependencyManager.GetService<ScreenFade>);
        private readonly Lazy<ZClassMenu> _zClassMenu = new(DependencyManager.GetService<ZClassMenu>);
        private readonly Lazy<EffectManager> _effectManager = new(DependencyManager.GetService<EffectManager>);
        private readonly Lazy<CommonUtils> _utils = new(DependencyManager.GetService<CommonUtils>);
        private readonly Lazy<RoundRatingNotify> _roundRatingNotify = new(DependencyManager.GetService<RoundRatingNotify>);
        private readonly Lazy<LifecycleManager> _lifecycleManager = new(DependencyManager.GetService<LifecycleManager>);
        private readonly Lazy<PlayerLifecycleManager> _playerLifecycleManager = new(DependencyManager.GetService<PlayerLifecycleManager>);
        private readonly Lazy<IWeaponRegistrator> _weaponRegistrator = new(DependencyManager.GetService<IWeaponRegistrator>);

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

            new ModelChanger(Core, _zombieManager.Value, _roundManager.Value, _utils.Value,
                DependencyManager.GetService<IOptions<ModelsConfig>>()).Load();
            new AdminMenu(Core, _roundManager.Value, _zombieManager.Value).Load();
            new SupplyBox().RegisterHooks();
            new ScoreResetService(Core).Initialize();
            new InfoNotifier(Core, DependencyManager.GetService<IOptions<InfoNotifierConfig>>()).Initialize();
        }

        public override void Unload()
        {
        }

        private void RegisterHooks()
        {
            _roundManager.Value.RegisterRounds();
            _knifeManager.Value.RegisterHooks();
            _zClassMenu.Value.RegisterHooks();
            _zombieManager.Value.RegisterHooks();
            _humanManager.Value.RegisterHooks();
            _effectManager.Value.RegisterHooks();
            _roundManager.Value.RegisterHooks();
        }

        private void LoadFeatures()
        {
            var config = DependencyManager.GetService<IOptions<ZombiePlagueCoreConfig>>().Value;
            if (config.DamageNotifyEnabled)
            {
                _damageNotify.Value.Start();
            }

            if (config.KnockbackEnabled)
            {
                _knockback.Value.Start();
            }

            if (config.MoneySystemEnabled)
            {
                _moneySystem.Value.Start();
            }

            if (config.ScreenFadeEnable)
            {
                _screenFade.Value.Start();
            }

            if (config.RoundRatingNotify)
            {
                _roundRatingNotify.Value.Start();
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
}