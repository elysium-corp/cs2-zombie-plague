using ZPCore.Data.Extensions;
using ZPCore.Data.Managers;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace ZPCore.Data.Plugins.AdminMenu;

internal class AdminMenu(ISwiftlyCore core, RoundManager roundManager, ZombieManager zombieManager)
{
    public void Load()
    {
        core.GameEvent.HookPost<EventPlayerChat>(PlayerChatEvent);
    }

    private HookResult PlayerChatEvent(EventPlayerChat @event)
    {
        var player = @event.UserIdPlayer;

        if (@event.Text == "!admin")
        {
            OpenMainMenu(player);
        }

        return HookResult.Continue;
    }

    private void OpenMainMenu(IPlayer player)
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Админ меню");
        AddButtonOption(menu, EndWarmup, "Выключить вармап");
        AddButtonOption(menu, RestartGame, "Начать игру заново");
        AddButtonOption(menu, GiveMoney, "Выдать себе 5000$");
        AddSubMenuOption(menu, OpenZombieMenu(player), "Сделать зомби");
        AddSubMenuOption(menu, OpenWeaponMenu(player), "Взять оружие");
        core.MenusAPI.OpenMenuForPlayer(player, menu.Build());
    }

    private IMenuAPI OpenZombieMenu(IPlayer player)
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Зомби меню");

        var allPlayers = core.PlayerManager.GetAlive();
        foreach (var p in allPlayers)
        {
            if (!p.IsInfected())
            {
                var option = new ButtonMenuOption(p.Controller.PlayerName);
                option.Click += async (sender, args) =>
                {
                    core.Scheduler.NextTickAsync(() =>
                    {
                        zombieManager.CreateZombie(p);
                    });
                    await Task.CompletedTask;
                };
                menu.AddOption(option);
            }
        }

        return menu.Build();
    }
    
    private IMenuAPI OpenWeaponMenu(IPlayer player)
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Арсенал");
        var option1 = new ButtonMenuOption("Взять ак");
        option1.Click += (sender, args) =>
        {
            GiveWeapon(args, "weapon_ak");
            return ValueTask.CompletedTask;
        };
        menu.AddOption(option1);
        
        var option2 = new ButtonMenuOption("Взять заморозку");
        option2.Click += (sender, args) =>
        {
            GiveWeapon(args, "weapon_hegrenade");
            return ValueTask.CompletedTask;
        };
        menu.AddOption(option2);
        
        var option3 = new ButtonMenuOption("Взять барьер");
        option3.Click += (sender, args) =>
        {
            GiveWeapon(args, "weapon_decoy");
            return ValueTask.CompletedTask;
        };
        menu.AddOption(option3);
        
        return menu.Build();
    }

    private void AddButtonOption(IMenuBuilderAPI menu, Func<MenuOptionClickEventArgs, Task> handler, string title)
    {
        var option = new ButtonMenuOption(title);
        option.Click += async (sender, args) => handler(args);
        menu.AddOption(option);
    }

    private void AddSubMenuOption(IMenuBuilderAPI menu, IMenuAPI subMenu, string title)
    {
        var option = new SubmenuMenuOption(title, subMenu);
        menu.AddOption(option);
    }

    private Task EndWarmup(MenuOptionClickEventArgs args)
    {
        core.Engine.ExecuteCommandAsync("mp_warmup_end");
        return Task.CompletedTask;
    }
    
    private Task RestartGame(MenuOptionClickEventArgs args)
    {
        core.Engine.ExecuteCommandAsync("mp_restartgame 1");
        return Task.CompletedTask;
    }

    private Task GiveMoney(MenuOptionClickEventArgs args)
    {
        var playerController = args.Player.Controller;
        playerController.InGameMoneyServices?.Account += 5000;
        playerController.InGameMoneyServices?.AccountUpdated();

        return Task.CompletedTask;
    }

    private void GiveWeapon(MenuOptionClickEventArgs args, string weaponName)
    {
        core.Scheduler.NextTickAsync(() =>
        {
            var player = args.Player;
            player.PlayerPawn?.ItemServices?.GiveItem(weaponName);
        });
    }
}