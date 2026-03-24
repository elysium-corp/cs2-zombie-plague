using ZPCore.Config.Weapon;
using ZPCore.Data.Extensions;
using ZPCore.Data.Weapons.Knifes;
using ZPCore.Utils.Extensions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZPCore.Data.Managers;

internal class KnifeManager(ISwiftlyCore core, IKnifeFactory factory, IOptions<KnifeConfig> config)
{
    private readonly Dictionary<IPlayer, IKnife> _playerKnifes = [];
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
    
    private void OnEntityTakeDamage(IOnEntityTakeDamageEvent @event)
    {
        var attacker = @event.Info.Attacker.ResolvePlayerFromHandle();

        if (attacker == null || !attacker.IsValid || !attacker.IsAlive || attacker.IsInfected())
        {
            return;
        }
        
        var victim = @event.Entity.Address.FindPlayerByPawnAddress();

        if (victim == null || !victim.IsValid || !victim.IsAlive)
        {
            return;
        }

        if (@event.Info.DamageType != DamageTypes_t.DMG_SLASH)
        {
            return;
        }

        var weapon = attacker.PlayerPawn?.WeaponServices?.ActiveWeapon.Value;

        if (weapon == null || !weapon.DesignerName.Contains("knife"))
        {
            return;
        }
        
        var knife = GetPlayerKnife(attacker);

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

        ApplyKnifeProperties(player);
        
        return HookResult.Continue;
    }

    private HookResult PlayerEquipEvent(EventItemEquip @event)
    {
        var player = @event.UserIdPlayer;
        
        if (player == null || !player.IsValid || player.IsInfected())
        {
            return HookResult.Continue;
        }
        
        if (@event.Item != "knife")
        {
            ApplyDefaultProperties(player);
            
            return HookResult.Continue;
        }

        ApplyKnifeProperties(player);

        return HookResult.Continue;
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

    public IKnife GetPlayerKnife(IPlayer player)
    {
        if (_playerKnifes.TryGetValue(player, out var knife))
        {
            return knife;
        }
        
        return _playerKnifes[player] = factory.Create<GravityKnifeWeapon>();
    }

    public void GiveKnife(IPlayer player)
    {
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (player.IsInfected())
            {
                return;
            }
            
            var playerPawn = player.PlayerPawn;
            
            if (playerPawn == null || !player.IsValid)
            {
                return;
            }
            
            var weaponService = playerPawn.WeaponServices;
            var itemService = playerPawn.ItemServices;
            
            if (weaponService == null || itemService == null)
            {
                return;
            }

            weaponService.RemoveWeaponByDesignerName(DefaultKnifeName);
            itemService.GiveItem(CustomKnifeName);

            var knife = GetPlayerKnife(player);
            var playerKnife = weaponService.MyValidWeapons.ToList().Find(w => w.DesignerName.Contains("knife"));

            if (playerKnife == null)
            {
                return;
            }
            
            playerKnife.SetModel(knife!.Model);
            core.Scheduler.NextTick(()=>weaponService.SelectWeapon(playerKnife));
        });
    }

    private void ApplyKnifeProperties(IPlayer player)
    {
        var knife = GetPlayerKnife(player);
        
        player.SetSpeed(knife.Speed);
        player.SetGravity(knife.Gravity);
    }

    private void ApplyDefaultProperties(IPlayer player)
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

            _playerKnifes[player] = factory.Create<T>();
            
            GiveKnife(player);
        };
        
        builder.AddOption(button);
    }
}