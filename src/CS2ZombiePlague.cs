using CS2ZombiePlague.Config;
using CS2ZombiePlague.Data;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Rounds;
using CS2ZombiePlague.Di;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;

namespace CS2ZombiePlague
{
    [PluginMetadata(Id = "CS2ZombiePlague", Version = "1.0.0", Name = "CS2ZombiePlague", Author = "illusion & fdrinv",
        Description = "Zombie Plague mode for CS2")]
    public partial class CS2ZombiePlague(ISwiftlyCore core) : BasePlugin(core)
    {
        private readonly Lazy<RoundManager> _roundManager = new(DependencyManager.GetService<RoundManager>);
        private readonly Lazy<ZombieManager> _zombieManager = new(DependencyManager.GetService<ZombieManager>);
        private readonly Lazy<WeaponManager> _weaponManager = new(DependencyManager.GetService<WeaponManager>);
        private readonly Lazy<KnifeManager> _knifeManager = new(DependencyManager.GetService<KnifeManager>);
        private readonly Lazy<Knockback> _knockback = new(DependencyManager.GetService<Knockback>);
        private readonly Lazy<DamageNotify> _damageNotify = new(DependencyManager.GetService<DamageNotify>);
        private readonly Lazy<MoneySystem> _moneySystem = new(DependencyManager.GetService<MoneySystem>);
        private readonly Lazy<ScreenFade> _screenFade = new(DependencyManager.GetService<ScreenFade>);
        private readonly Lazy<ZClassMenu> _zClassMenu = new(DependencyManager.GetService<ZClassMenu>);
        private readonly Lazy<CommonUtils> _utils = new(DependencyManager.GetService<CommonUtils>);
        
        public override void Load(bool hotReload)
        {
            if (hotReload)
            {
                DependencyManager.Dispose();
            }

            DependencyManager.Load(Core);

            _roundManager.Value.RegisterRounds();
            _weaponManager.Value.RegisterWeapons();
            _knifeManager.Value.RegisterHooks();
            _zClassMenu.Value.RegisterHooks();

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

            new AdminMenu(Core, _roundManager.Value, _zombieManager.Value).Load();

            Core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
            Core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        }

        public override void Unload()
        {
        }

        private HookResult OnRoundStart(EventRoundStart @event)
        {
            var zombieManager = _zombieManager.Value;
            var roundManager = _roundManager.Value;
            var utils = _utils.Value;
            
            zombieManager.RemoveAll();
            roundManager.CancelToken();
            utils.MoveAllPlayersToTeam(Team.CT);
            utils.AllResetRenderColor();

            roundManager.SetRound(new None());

            if (roundManager.RoundIsAvailable())
            {
                roundManager.Start();
            }

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        private HookResult OnPlayerHurt(EventPlayerHurt @event)
        {
            var roundManager = _roundManager.Value;
            var victim = Core.PlayerManager.GetPlayer(@event.UserId);
            if (victim == null)
            {
                return HookResult.Continue;
            }

            if (roundManager.IsNoneRound())
            {
                return HookResult.Stop;
            }

            return HookResult.Continue;
        }

        private HookResult OnRoundEnd(EventRoundEnd @event)
        {
            var roundManager = _roundManager.Value;
            if (roundManager.GetRound() != null)
            {
                roundManager.GetRound()?.End();
            }

            return HookResult.Continue;
        }

        [EventListener<EventDelegates.OnPrecacheResource>]
        private void OnPrecacheResource(IOnPrecacheResourceEvent @event)
        {
            @event.AddItem("characters/models/s2ze/zombie_frozen/zombie_frozen.vmdl");
            @event.AddItem("characters/models/kolka/2025/bull/bull.vmdl");
            @event.AddItem("characters/models/kolka/2025/hazmat/hazmat.vmdl");
            @event.AddItem("characters/models/kolka/2025/lurker/lurker.vmdl");
            @event.AddItem("weapons/nozb1/valogun/knife/sovereign_tactical/sovereign_tactical_ag2.vmdl");
            @event.AddItem("weapons/nozb1/valogun/knife/ejderbicak_cord/ejderbicak_cord_ag2.vmdl");
            @event.AddItem("weapons/nozb1/valogun/knife/ashen_kukri/ashen_kukri_ag2.vmdl");
            @event.AddItem("particles/kolka/part1.vpcf");
            @event.AddItem("particles/kolka/part2.vpcf");
            @event.AddItem("particles/kolka/part3.vpcf");
            @event.AddItem("particles/kolka/part4.vpcf");
            @event.AddItem("particles/kolka/part5.vpcf");
            @event.AddItem("particles/kolka/part6.vpcf");
            @event.AddItem("particles/kolka/part7.vpcf");
            @event.AddItem("particles/kolka/part8.vpcf");
            @event.AddItem("particles/kolka/part9.vpcf");
            @event.AddItem("particles/kolka/part10.vpcf");
            @event.AddItem("particles/kolka/part11.vpcf");
            @event.AddItem("particles/kolka/part12.vpcf");
            @event.AddItem("particles/kolka/part13.vpcf");
            @event.AddItem("particles/kolka/part14.vpcf");
            @event.AddItem("particles/kolka/part15.vpcf");
            @event.AddItem("particles/kolka/part16.vpcf");
            @event.AddItem("particles/kolka/part17.vpcf");
            @event.AddItem("particles/kolka/part18.vpcf");
            @event.AddItem("soundevents/soundevents_zombieplague.vsndevts");
            @event.AddItem("sounds/cs2/countdown/countdown.vsnd");
            @event.AddItem("sounds/cs2/weapons/frostnade/frostnade_detonate.vsnd");
            @event.AddItem("sounds/cs2/weapons/frostnade/frostnade_end.vsnd");
            @event.AddItem("sounds/cs2/weapons/frostnade/frostnade_hit.vsnd");
            @event.AddItem("sounds/cs2/zombie/zombie_pressure.vsnd");
            @event.AddItem("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
        }

        [EventListener<EventDelegates.OnWeaponServicesCanUseHook>]
        private void OnItemServicesCanAcquireHook(IOnWeaponServicesCanUseHookEvent @event)
        {
            var player = Core.PlayerManager.GetPlayer((int)@event.WeaponServices.Pawn.Controller.EntityIndex - 1);
            if (player != null && player.IsValid)
            {
                if (player.IsInfected() && @event.Weapon.DesignerName != "weapon_knife")
                {
                    @event.SetResult(false);
                }
            }
        }
    }
}