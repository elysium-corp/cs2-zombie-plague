using CS2ZombiePlague.Config.Weapon;
using CS2ZombiePlague.Data.Extensions;
using CS2ZombiePlague.Data.Weapons.Knifes;
using Microsoft.Extensions.Options;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Managers;

public class KnifeManager(
    ISwiftlyCore core,
    IKnifeFactory factory,
    IOptions<KnifeConfig> config)
{
    private readonly Dictionary<int, IKnife> _playerKnifes = new();
    private IMenuAPI _menuApi = null!;

    private const float DefaultSpeed = 250f;
    private const float DefaultGravity = 800f;

    public void RegisterHooks()
    {
        core.GameEvent.HookPost<EventItemEquip>(PlayerEquipEvent);
        core.GameEvent.HookPost<EventPlayerChat>(PlayerChatEvent);
        core.GameEvent.HookPost<EventPlayerSpawn>(PlayerSpawnEvent);
        _menuApi = CreateMenu();
    }

    public IKnife? GetCurrentKnife(int playerId) => _playerKnifes.GetValueOrDefault(playerId, null);

    public void GiveKnife(IPlayer player)
    {
        core.Scheduler.NextWorldUpdateAsync(() =>
        {
            var pawn = player.PlayerPawn;
            if (pawn == null || !player.Controller.PawnIsAlive)
            {
                return;
            }

            if (player.IsInfected())
            {
                return;
            }

            var weaponService = pawn.WeaponServices;
            var itemService = pawn.ItemServices;
            if (weaponService == null || itemService == null)
            {
                return;
            }

            if (!_playerKnifes.ContainsKey(player.PlayerID))
                SetDefaultKnife(player.PlayerID);

            var knife = GetCurrentKnife(player.PlayerID);

            weaponService.RemoveWeaponByDesignerName("weapon_knife");
            itemService.GiveItem("weapon_knife_t");

            foreach (var weapon in weaponService.MyValidWeapons)
            {
                if (!weapon.DesignerName.Contains("knife"))
                    continue;

                weapon.SetModel(knife.Model);
                weaponService.SelectWeapon(weapon);

                break;
            }
        });
    }

    private void SetDefaultKnife(int playerId) => _playerKnifes[playerId] = factory.Create<GravityKnifeWeapon>();

    private HookResult PlayerSpawnEvent(EventPlayerSpawn @event)
    {
        if (!@event.UserIdPlayer.IsValid)
        {
            return HookResult.Continue;
        }

        GiveKnife(@event.UserIdPlayer);
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
            if (@args.Player.IsInfected())
            {
                return;
            }

            _playerKnifes[args.Player.PlayerID] = factory.Create<T>();
            GiveKnife(args.Player);
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