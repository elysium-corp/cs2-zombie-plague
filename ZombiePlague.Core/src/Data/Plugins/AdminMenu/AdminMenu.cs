using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menus;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Rounds.Registrator;

namespace ZombiePlague.Core.Data.Plugins.AdminMenu;

internal sealed class AdminMenu(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IRoundManager roundManager,
    IRoundRegistrator roundRegistrator,
    IRoundFactory roundFactory
)
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

        if (player is not { IsValid: true })
        {
            return;
        }

        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Админ меню");

        AddButtonOption(menu, EndWarmup, "Выключить вармап");
        AddButtonOption(menu, RestartGame, "Начать игру заново");
        AddSubMenuOption(menu, SetRound(), "Управление игровыми раундами");
        AddButtonOption(menu, GiveMoney, "Выдать себе 5000$");
        AddSubMenuOption(menu, CreateZombieMenu(), "Сделать зомби");
        AddSubMenuOption(menu, CreateWeaponMenu(), "Взять оружие");

        core.MenusAPI.OpenMenuForPlayer(player, menu.Build());
    }

    private IMenuAPI CreateZombieMenu()
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Зомби меню");

        foreach (var player in core.PlayerManager.GetAlive())
        {
            if (playerManager.IsZombie(player))
            {
                continue;
            }

            var option = new ButtonMenuOption(player.Controller.PlayerName);

            option.Click += (_, _) =>
            {
                core.Scheduler.NextTick(() =>
                {
                    if (player.IsValid)
                    {
                        playerManager.TryInfect(player);
                    }
                });

                return ValueTask.CompletedTask;
            };

            menu.AddOption(option);
        }

        return menu.Build();
    }

    private IMenuAPI CreateWeaponMenu()
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle("Арсенал");

        AddWeaponOption(menu, "Взять AK-47", "weapon_ak47");
        AddWeaponOption(menu, "Взять заморозку", "weapon_hegrenade");
        AddWeaponOption(menu, "Взять барьер", "weapon_decoy");

        return menu.Build();
    }

    private static void AddButtonOption(
        IMenuBuilderAPI menu,
        Func<MenuOptionClickEventArgs, Task> handler,
        string title
    )
    {
        var option = new ButtonMenuOption(title);

        option.Click += async (_, args) => await handler(args);

        menu.AddOption(option);
    }

    private static void AddSubMenuOption(
        IMenuBuilderAPI menu,
        IMenuAPI subMenu,
        string title
    )
    {
        menu.AddOption(new SubmenuMenuOption(title, subMenu));
    }

    private void AddWeaponOption(
        IMenuBuilderAPI menu,
        string title,
        string weaponName
    )
    {
        var option = new ButtonMenuOption(title);

        option.Click += (_, args) =>
        {
            var player = args.Player;

            core.Scheduler.NextTick(() =>
            {
                if (player.IsValid)
                {
                    player.PlayerPawn?.ItemServices?.GiveItem(weaponName);
                }
            });

            return ValueTask.CompletedTask;
        };

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

    private static Task GiveMoney(MenuOptionClickEventArgs args)
    {
        var moneyServices = args.Player.Controller.InGameMoneyServices;

        if (moneyServices is not null)
        {
            moneyServices.Account += 5000;
            moneyServices.AccountUpdated();
        }

        return Task.CompletedTask;
    }

    private IMenuAPI SetRound()
    {
        var menu = core.MenusAPI.CreateBuilder().Design.SetMenuTitle("Управление игровыми раундами");
        var currentRound = roundManager.CurrentRound?.Name ?? "Не определен";
        var nextRound = roundManager.NextRound?.Name ?? "Не определен";
        var currentRoundTextOption = new TextMenuOption
        {
            Text = $"Текущий раунд: {currentRound}"
        };
        var nextRoundTextOption = new TextMenuOption
        {
            Text = $"Следующий раунд: {nextRound}"
        };

        menu.AddOption(currentRoundTextOption);
        menu.AddOption(nextRoundTextOption);
        menu.AddOption(new SubmenuMenuOption("Установить текущий раунд", SetCurrentRound()));
        menu.AddOption(new SubmenuMenuOption("Установить следующий раунд", SetNextRound()));

        return menu.Build();
    }

    private IMenuAPI SetCurrentRound()
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle($"Установить текущий раунд");

        foreach (var round in roundRegistrator.GetAll())
        {
            var enable = round is { Enable: true, Weight: > 0 } ? "[+]" : "[-]";
            var text = $"{round.Name} {enable}";
            var option = new ButtonMenuOption(text);

            option.Click += (_, args) =>
            {
                args.Player.SendChatAsync($"Установлен текущий раунд: {round.Name}");
                
                roundManager.SelectNextRound(roundFactory.Create(round));

                return ValueTask.CompletedTask;
            };

            menu.AddOption(option);
        }

        return menu.Build();
    }

    private IMenuAPI SetNextRound()
    {
        var menu = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle($"Установить следующий раунд");

        foreach (var round in roundRegistrator.GetAll())
        {
            var enable = round is { Enable: true, Weight: > 0 } ? "[+]" : "[-]";
            var text = $"{round.Name} {enable}";
            var option = new ButtonMenuOption(text);

            option.Click += (_, args) =>
            {
                args.Player.SendChatAsync($"Установлен следующий раунд: {round.Name}");
                
                roundManager.SelectNextRound(roundFactory.Create(round));

                return ValueTask.CompletedTask;
            };

            menu.AddOption(option);
        }

        return menu.Build();
    }
}