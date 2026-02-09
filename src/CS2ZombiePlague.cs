using CS2ZombiePlague.Config;
using CS2ZombiePlague.Config.models;
using CS2ZombiePlague.Data;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Lifecycle;
using CS2ZombiePlague.Data.Managers;
using CS2ZombiePlague.Data.Rounds;
using CS2ZombiePlague.Data.Weapons.Shotguns;
using CS2ZombiePlague.Di;
using CS2ZombiePlague.Service;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameEvents;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Plugins;
using SwiftlyS2.Shared.SchemaDefinitions;
using EventDelegates = SwiftlyS2.Shared.Events.EventDelegates;

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
        private readonly Lazy<WeaponService> _weaponService = new(DependencyManager.GetService<WeaponService>);
        private readonly Lazy<CommonUtils> _utils = new(DependencyManager.GetService<CommonUtils>);
        private readonly Lazy<RoundRatingNotify> _roundRatingNotify = new(DependencyManager.GetService<RoundRatingNotify>);
        private readonly Lazy<LifecycleManager> _lifecycleManager = new(DependencyManager.GetService<LifecycleManager>);
        private readonly Lazy<PlayerLifecycleManager> _playerLifecycleManager = new(DependencyManager.GetService<PlayerLifecycleManager>);

        public override void Load(bool hotReload)
        {
            if (hotReload)
            {
                DependencyManager.Dispose();
                _lifecycleManager.Value.Dispose();
            }

            DependencyManager.Load(Core);
            
            _lifecycleManager.Value.Initialize();
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
            
            if (config.RoundRatingNotify)
            {
                _roundRatingNotify.Value.Start();
            }

            new ModelChanger(Core, _zombieManager.Value, _roundManager.Value, _utils.Value,
                DependencyManager.GetService<IOptions<ModelsConfig>>()).Load();
            new AdminMenu(Core, _roundManager.Value, _zombieManager.Value).Load();

            RegisterCommands();
            
            Core.GameEvent.HookPre<EventRoundStart>(OnRoundStart);
            Core.GameEvent.HookPost<EventRoundEnd>(OnRoundEnd);
        }

        public override void Unload()
        {
        }
        
        private void RegisterCommands()
        {
            Core.Command.RegisterCommand(
                commandName: "gun",
                handler: GunHandler,
                registerRaw: true
            );
            
            Core.Command.RegisterCommand(
                commandName: "debug",
                handler: DebugHandler,
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
            
            Core.PlayerManager.SendChat($"Команда !{context.CommandName} вызвалась!");
            _weaponService.Value.GiveWeapon<Frostbyte, CWeaponMP7>(player);
        }
        
        private void DebugHandler(ICommandContext context)
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

            var weaponService = _weaponService.Value;
            var weapons = weaponService.GetAllWeapons();
            var numberOfWeapons = weapons.Count;
            
            Core.PlayerManager.SendChat($"WeaponService на данный момент имеет {numberOfWeapons} пушек");

            for (int i = 0; i < numberOfWeapons; i++)
            {
                Core.PlayerManager.SendChat($"{i + 1}. {weapons[i].DisplayName} (index = {weapons[i].InheritorWeapon?.Index})");
            }

            var playerLifecycleManager = _playerLifecycleManager.Value;
            
            foreach (var playerLifecycle in playerLifecycleManager.GetPlayers())
            {
                Core.PlayerManager.SendChat($"playerLifecycle = {playerLifecycle.Player.Controller.PlayerName}");
            }
        }

        [GameEventHandler(HookMode.Pre)]
        private HookResult OnMapChange(EventMapTransition @event)
        {
            return HookResult.Continue;
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
        
        [GameEventHandler(HookMode.Pre)]
        private HookResult OnGameRestart(EventCsPreRestart @event)
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
            @event.AddItem("weapons/nozb1/valogun/knife/oni_katana_tactical/oni_katana_tactical_ag2.vmdl");
            @event.AddItem("characters/models/nozb1/nemesis_player_model/nemesis_player_model.vmdl");
            @event.AddItem("characters/models/nozb1/zhunter_player_model/zhunter_player_model.vmdl");
            @event.AddItem("characters/models/nozb1/nanosuit_player_model/nanosuit_player_model.vmdl");
            @event.AddItem("particles/kolka/part1.vpcf");
            @event.AddItem("particles/barrier_nade.vpcf");
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
            @event.AddItem("particles/barrier_nade.vpcf");
            @event.AddItem("soundevents/soundevents_zombieplague.vsndevts");
            @event.AddItem("particles/explosions_fx/bumpmine_detonate_sparks.vpcf");
            @event.AddItem("particles/explosions_fx/bumpmine_detonate.vpcf");
            @event.AddItem("models/de_overpass/decorations/security_camera/security_camera_1_base.vmdl"); 
            @event.AddItem("sounds/cs2/countdown/countdown.vsnd");
            @event.AddItem("sounds/cs2/weapons/frostnade/frostnade_detonate.vsnd");
            @event.AddItem("sounds/cs2/weapons/frostnade/frostnade_end.vsnd");
            @event.AddItem("sounds/cs2/weapons/frostnade/frostnade_hit.vsnd");
            @event.AddItem("sounds/cs2/zombie/zombie_pressure.vsnd");
            @event.AddItem("models/props/de_dust/hr_dust/dust_soccerball/dust_soccer_ball001.vmdl");
            @event.AddItem("particles/ui/rank_carepackage_recieve.vpcf");
            @event.AddItem("particles/ui/ammohealthcenter/ui_hud_kill_burn_ringfire.vpcf");
            @event.AddItem("particles/ui/ammohealthcenter/ui_hud_kill_streaks_circle_flash.vpcf");
            @event.AddItem("particles/ui/hud/ui_mvp_winner_burst.vpcf");
            @event.AddItem("weapons/nozb1/valogun/araxys_bundle/araxys_sawedoff/araxys_sawedoff_ag2.vmdl");
            @event.AddItem("particles/weapons/cs_weapon_fx/weapon_tracers_taser.vpcf");
            @event.AddItem("particles/weapons/cs_weapon_fx/bumpmine_active.vpcf");
            @event.AddItem("particles/weapons/cs_weapon_fx/weapon_confetti_sparks_2.vpcf");
            @event.AddItem("particles/ui/ammohealthcenter/ui_hud_kill_elec_innerpoint.vpcf");
            @event.AddItem("weapons/luci/x3_m4a1/x3_m4a1_ag2.vmdl");
            @event.AddItem("characters/models/nozb1/chris_walker_player_model/chris_walker_player_model.vmdl");
            @event.AddItem("characters/models/nozb1/jason_player_model/jason_player_model.vmdl");
            @event.AddItem("characters/models/nozb1/zombie_stalker_player_model/zombie_stalker_player_model.vmdl");
            @event.AddItem("weapons/luci/car_ump45/car_ump45_ag2.vmdl");
            @event.AddItem("weapons/luci/eov_mp5/eov_mp5_ag2.vmdl");
            @event.AddItem("weapons/luci/parab_ssg/parab_ssg_ag2.vmdl");
            @event.AddItem("weapons/luci/psd_mp9/psd_mp9_ag2.vmdl");
        }

        [EventListener<EventDelegates.OnWeaponServicesCanUseHook>]
        private void OnItemServicesCanAcquireHook(IOnWeaponServicesCanUseHookEvent @event)
        {
            var pawn = @event.WeaponServices.Pawn;
            var player = core.PlayerManager.GetPlayerFromPawn(pawn);

            if (player is not { IsValid: true })
            {
                return;
            }
            
            var weaponName = @event.Weapon.DesignerName;

            if (player.IsInfected() && !weaponName.Contains("knife") && !weaponName.Contains("smoke"))
            {
                @event.SetResult(false);
            }
        }
        
        [GameEventHandler(HookMode.Pre)]
        private HookResult EventPlayerDisconnect(EventPlayerDisconnect @event)
        {
            var player = @event.UserIdPlayer;
            if (player == null || player.IsFakeClient)
                return HookResult.Continue;

            if (_zombieManager.Value.GetZombie(player.PlayerID) != null)
            {
                _zombieManager.Value.Remove(player);
            }

            return HookResult.Continue;
        }

        [GameEventHandler(HookMode.Pre)]
        private HookResult EventPlayerConnectFull(EventPlayerConnectFull @event)
        {
            var player = @event.UserIdPlayer;
            if (player == null || player.IsFakeClient)
                return HookResult.Continue;

            if (_roundManager.Value.IsNoneRound())
            {
                player.SwitchTeam(Team.CT);
                player.Respawn();
            }

            return HookResult.Continue;
        }
    }
}