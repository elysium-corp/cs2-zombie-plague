using CS2ZombiePlague.Config.Weapon;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Weapons.Knifes;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Managers;

public class KnifeManager(
    ISwiftlyCore core,
    CommonUtils commonUtils,
    IKnifeFactory factory,
    IOptions<KnifeConfig> config)
{
    private readonly Dictionary<int, IKnife> _playerKnifes = new();
    private IMenuAPI _menuApi = null!;

    private const float DefaultSpeed = 250f;
    private const float DefaultGravity = 800f;

    private const string DefaultKnifeName = "weapon_knife";
    private const string CustomKnifeName = "weapon_knife_t";

    public void RegisterHooks()
    {
        core.GameEvent.HookPost<EventItemEquip>(PlayerEquipEvent);
        core.GameEvent.HookPost<EventPlayerChat>(PlayerChatEvent);
        core.GameEvent.HookPost<EventPlayerSpawn>(PlayerSpawnEvent);
        core.GameEvent.HookPost<EventPlayerHurt>(PlayerHurtEvent);
        
        core.Event.OnEntityTakeDamage += OnEntityTakeDamage;
        
        _menuApi = CreateMenu();
    }

    public IKnife? GetCurrentKnife(int playerId) => _playerKnifes!.GetValueOrDefault(playerId, null);

    public void GiveKnife(IPlayer player)
    {
        core.Scheduler.NextWorldUpdate(() =>
        {
            var pawn = player.PlayerPawn;
            if (pawn == null || !player.IsValid)
            {
                return;
            }

            if (player.IsInfected())
            {
                return;
            }

            if (!_playerKnifes.ContainsKey(player.PlayerID))
                SetDefaultKnife(player.PlayerID);
            
            var weaponService = pawn.WeaponServices;
            var itemService = pawn.ItemServices;
            if (weaponService == null || itemService == null)
            {
                return;
            }

            weaponService.RemoveWeaponByDesignerName(DefaultKnifeName);
            itemService.GiveItem(CustomKnifeName);

            var knife = GetCurrentKnife(player.PlayerID);
            var playerKnife = weaponService.MyValidWeapons.ToList().Find(w => w.DesignerName.Contains("knife"));
            if (playerKnife != null)
            {
                playerKnife.SetModel(knife!.Model);
                core.Scheduler.NextTick(()=>weaponService.SelectWeapon(playerKnife));
            }
        });
    }

    private void SetDefaultKnife(int playerId) => _playerKnifes[playerId] = factory.Create<GravityKnifeWeapon>();

    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = commonUtils.ResolvePlayerFromHandle(@event.Info.Attacker);

        if (attacker is not { IsValid: true } || !attacker.IsAlive || attacker.IsInfected()) return;
        
        var victim = commonUtils.FindPlayerByPawnAddress(@event.Entity.Address);
        
        if (victim is not { IsValid: true } || !victim.IsAlive) return;
        
        if(@event.Info.DamageType != DamageTypes_t.DMG_SLASH) return;

        var weapon = attacker.PlayerPawn?.WeaponServices?.ActiveWeapon.Value;
        if(weapon == null || !weapon.DesignerName.Contains("knife")) return;
        
        var knife = GetCurrentKnife(attacker.PlayerID);
        if (knife == null) return;

        @event.Info.Damage *= knife.DamageMultiplier;
    }
    
    private HookResult PlayerSpawnEvent(EventPlayerSpawn @event)
    {
        var player = @event.UserIdPlayer;
        if (player == null || !player.IsValid)
        {
            return HookResult.Continue;
        }

        GiveKnife(player);
        return HookResult.Continue;
    }
    
    private HookResult PlayerHurtEvent(EventPlayerHurt @event)
    {
        var player = @event.UserIdPlayer;
        if (player == null || !player.IsValid || player.IsInfected())
        {
            return HookResult.Continue;
        }

        var weaponService = player.PlayerPawn?.WeaponServices;
        var activeWeapon = weaponService?.ActiveWeapon.Value;
        if (activeWeapon == null)
        {
            return HookResult.Continue;
        }
        
        var isKnife = activeWeapon.DesignerName.Contains("knife");
        if (!isKnife)
        {
            return HookResult.Continue;
        }

        var playerId = player.PlayerID;
        if (!_playerKnifes.ContainsKey(playerId))
            SetDefaultKnife(playerId);

        var knife = GetCurrentKnife(playerId);

        SetKnifeProperties(knife!, player);
        
        return HookResult.Continue;
    }

    private HookResult PlayerEquipEvent(EventItemEquip @event)
    {
        var player = @event.UserIdPlayer;
        var pawn = player.RequiredPawn;

        if (!pawn.IsValid || player.IsInfected())
            return HookResult.Continue;

        if (@event.Item != "knife")
        {
            SetDefaultProperties(player);
            return HookResult.Continue;
        }

        var playerId = player.PlayerID;
        if (!_playerKnifes.ContainsKey(playerId))
            SetDefaultKnife(playerId);

        var knife = GetCurrentKnife(playerId);

        SetKnifeProperties(knife!, player);

        return HookResult.Continue;
    }

    private void SetKnifeProperties(IKnife knife, IPlayer player)
    {
        player.SetSpeed(knife.Speed);
        player.SetGravity(knife.Gravity);
    }

    private void SetDefaultProperties(IPlayer player)
    {
        player.SetSpeed(DefaultSpeed);
        player.SetGravity(DefaultGravity);
    }

    private IMenuAPI CreateMenu()
    {
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Выбери нож")
            .EnableSound();

        AddKnifeOption<KnockbackKnifeWeapon>(builder, config.Value.Knockback);
        AddKnifeOption<SpeedKnifeWeapon>(builder, config.Value.Speed);
        AddKnifeOption<GravityKnifeWeapon>(builder, config.Value.Gravity);
        AddKnifeOption<VipKnifeWeapon>(builder, config.Value.Vip);

        return builder.Build();
    }

    private void AddKnifeOption<T>(IMenuBuilderAPI builder, IKnifeConfig cfg) where T : IKnife
    {
        var button = new ButtonMenuOption($"{cfg.DisplayName} {cfg.Description}");
        button.Click += async (_, args) =>
        {
            var player = args.Player;
            if (player.IsInfected())
            {
                return;
            }

            _playerKnifes[player.PlayerID] = factory.Create<T>();
            GiveKnife(player);
        };
        builder.AddOption(button);
    }

    private HookResult PlayerChatEvent(EventPlayerChat @event)
    {
        var player = @event.UserIdPlayer;
        if (@event.Text == "!knife" && !player.IsInfected())
        {
            core.MenusAPI.OpenMenuForPlayer(player, _menuApi);
        }

        return HookResult.Continue;
    }
}