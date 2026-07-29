using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Rounds;

namespace ZombiePlague.Core.Data.Plugins.AdminMenu;

internal class AdminMenu(ISwiftlyCore core, RoundManager roundManager, IZombieManager zombieManager)
{
    public void Load()
    {
        core.Command.RegisterCommand(
            commandName: "admin",
            handler: OpenMainMenu,
            registerRaw: true
        );
    }

    private void OpenMainMenu(ICommandContext context)
    {
        var player = context.Sender;

        if (player == null)
        {
            return;
        }
        
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Админ меню");
        
        AddButtonOption(menu, EndWarmup, "Выключить вармап");
        AddButtonOption(menu, RestartGame, "Начать игру заново");
        AddButtonOption(menu, GiveMoney, "Выдать себе 5000$");
        AddSubMenuOption(menu, OpenRoundMenu(player), "Установить режим");
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
            if (zombieManager.GetZombie(p) == null)
            {
                var option = new ButtonMenuOption(p.Controller.PlayerName);
                
                option.Click += async (_, _) =>
                {
                    await core.Scheduler.NextTickAsync(() =>
                    {
                        zombieManager.CreateZombie(p);
                    });
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
    
    private IMenuAPI OpenRoundMenu(IPlayer player)
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Установить режим");

        if (!roundManager.IsNoneRound())
        {
            return menu.Build();
        }
        
        var registeredRounds = roundManager.GetRegisteredRounds();

        foreach (var round in registeredRounds)
        {
            if (round is None)
            {
                continue;
            }
            
            var option = new ButtonMenuOption(round.Name);
            
            option.Click += (sender, args) =>
            {
                roundManager.SetRound(round);
                
                core.MenusAPI.CloseActiveMenu(args.Player);
                
                return ValueTask.CompletedTask;
            };
            
            menu.AddOption(option);
        }
        
        return menu.Build();
    }

    private void AddButtonOption(IMenuBuilderAPI menu, Func<MenuOptionClickEventArgs, Task> handler, string title)
    {
        var option = new ButtonMenuOption(title);
        
        option.Click += async (_, args) => await handler(args);
        
        menu.AddOption(option);
    }

    private void AddSubMenuOption(IMenuBuilderAPI menu, IMenuAPI subMenu, string title)
    {
        var option = new SubmenuMenuOption(title, subMenu);
        
        menu.AddOption(option);
    }

    private Task EndWarmup(MenuOptionClickEventArgs args)
    {
        return core.Engine.ExecuteCommandAsync("mp_warmup_end");
    }
    
    private Task RestartGame(MenuOptionClickEventArgs args)
    {
        return core.Engine.ExecuteCommandAsync("mp_restartgame 1");
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
