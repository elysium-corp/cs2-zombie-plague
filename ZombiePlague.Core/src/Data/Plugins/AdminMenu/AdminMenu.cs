using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Rounds.Registrator;
using ZombiePlague.Core.Utils.Helpers;

namespace ZombiePlague.Core.Data.Plugins.AdminMenu;

internal sealed class AdminMenu(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    IRoundManager roundManager,
    IRoundRegistrator roundRegistrator,
    IRoundFactory roundFactory
)
{
    private const string AccentColor = "#7DD3FC";
    private const string SuccessColor = "#86EFAC";
    private const string WarningColor = "#FDBA74";
    private const string DisabledColor = "#94A3B8";
    private const string DangerColor = "#FCA5A5";
    private const string TextColor = "#E2E8F0";
    
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
        var menu = core.MenusAPI
            .CreateBuilder()
            .Design.SetMenuTitle("Управление раундами");

        var currentRoundName =
            roundManager.CurrentRound?.Name ??
            (roundManager.IsPreparing ? "Подготовка" : "Нет активного раунда");

        var currentRoundColor =
            roundManager.CurrentRound is not null
                ? SuccessColor
                : WarningColor;

        var nextRoundName = roundManager.NextRound?.Name ?? "Автоматический выбор";

        var nextRoundColor = roundManager.NextRound is not null
                ? AccentColor
                : DisabledColor;

        menu.AddOption(
            new TextMenuOption
            {
                Text =
                    $"{HtmlHelper.TextWithColor("Текущий:", TextColor)} " + HtmlHelper.TextWithColor(currentRoundName, currentRoundColor)
            }
        );

        menu.AddOption(
            new TextMenuOption
            {
                Text = $"{HtmlHelper.TextWithColor("Следующий:", TextColor)} " + HtmlHelper.TextWithColor(nextRoundName, nextRoundColor)
            }
        );

        AddStartImmediatelyOption(menu);

        menu.AddOption(
            new SubmenuMenuOption(
                HtmlHelper.TextWithColor("➜ Выбрать следующий раунд", AccentColor),
                SetNextRound()
            )
        );

        return menu.Build();
    }

    private IMenuAPI SetNextRound()
{
    var menu = core.MenusAPI
        .CreateBuilder()
        .Design.SetMenuTitle("Следующий раунд");

    AddAutomaticRoundOption(menu);

    foreach (var roundConfig in roundRegistrator.GetAll())
    {
        var enabled = roundConfig is
        {
            Enable: true,
            Weight: > 0
        };

        if (!enabled)
        {
            var disabledOption =
                new ButtonMenuOption
                {
                    Text = HtmlHelper.TextWithColor($"{roundConfig.Name} • отключён", DisabledColor),
                    Enabled = false
                };

            menu.AddOption(disabledOption);

            continue;
        }

        var round = roundFactory.Create(roundConfig);

        var canStart = round.CanStart();

        var isSelected = roundManager.NextRound?.Id == round.Id;

        var status = isSelected ? " • выбран" : canStart ? " • доступен" : " • условия не выполнены";

        var color = canStart
            ? SuccessColor
            : WarningColor;

        var option = new ButtonMenuOption
        {
            Text = HtmlHelper.TextWithColor($"{round.Name}{status}", color),
            Enabled = true
        };

        option.Click += (_, args) =>
        {
            var player = args.Player;

            core.Scheduler.NextTick(() =>
            {
                if (!player.IsValid)
                {
                    return;
                }

                roundManager.SelectNextRound(round);

                if (round.CanStart())
                {
                    player.SendChatAsync(
                        $"{HtmlHelper.TextWithColor("[ADMIN]", AccentColor)} " + $"Следующий раунд: " + HtmlHelper.TextWithColor(round.Name, SuccessColor)
                    );
                }
                else
                {
                    player.SendChatAsync(
                        $"{HtmlHelper.TextWithColor("[ADMIN]", AccentColor)} " +
                        $"Следующий раунд: " + HtmlHelper.TextWithColor(round.Name, WarningColor) + ". Условия будут проверены перед запуском"
                    );
                }
            });

            return ValueTask.CompletedTask;
        };

        menu.AddOption(option);
    }

    return menu.Build();
}
    
    private void SendRandomRoundStartResult(IPlayer player, RoundStartResult result)
    {
        switch (result)
        {
            case RoundStartResult.Started:
            {
                var startedRound =
                    roundManager.CurrentRound;

                player.SendChatAsync(
                    $"{HtmlHelper.TextWithColor("[ADMIN]", AccentColor)} " +
                    $"Запущен раунд: " + HtmlHelper.TextWithColor(startedRound?.Name ?? "Неизвестно", SuccessColor)
                );

                break;
            }

            case RoundStartResult.NotPreparing:
            {
                player.SendChatAsync(
                    $"{HtmlHelper.TextWithColor("[ADMIN]", AccentColor)} " +
                    HtmlHelper.TextWithColor("Preparation уже завершён.", WarningColor)
                );

                break;
            }

            case RoundStartResult.CannotStart:
            {
                player.SendChatAsync(
                    $"{HtmlHelper.TextWithColor("[ADMIN]", AccentColor)} " +
                    HtmlHelper.TextWithColor("Не удалось подобрать раунд для запуска.", WarningColor)
                );

                break;
            }

            case RoundStartResult.Cancelled:
            {
                player.SendChatAsync(
                    $"{HtmlHelper.TextWithColor("[ADMIN]", AccentColor)} " +
                    HtmlHelper.TextWithColor("Запуск раунда был отменён.", DangerColor)
                );

                break;
            }
        }
    }
    
    private void AddStartImmediatelyOption(IMenuBuilderAPI menu)
    {
        var option = new ButtonMenuOption
        {
            Text = HtmlHelper.TextWithColor("⚡ Запустить немедленно", DangerColor),
            Enabled = roundManager.IsPreparing
        };

        option.Click += (_, args) =>
        {
            var player = args.Player;

            core.Scheduler.NextTick(() =>
            {
                if (!player.IsValid)
                {
                    return;
                }

                var result = roundManager.TryStartRandomRound();

                SendRandomRoundStartResult(
                    player,
                    result
                );
            });

            return ValueTask.CompletedTask;
        };

        menu.AddOption(option);
    }
    
    private void AddAutomaticRoundOption(IMenuBuilderAPI menu)
    {
        var isSelected = roundManager.NextRound is null;

        var text = isSelected
            ? "Автоматический выбор • выбран"
            : "Автоматический выбор";

        var option = new ButtonMenuOption
        {
            Text = HtmlHelper.TextWithColor(text, AccentColor)
        };

        option.Click += (_, args) =>
        {
            var player = args.Player;

            core.Scheduler.NextTick(() =>
            {
                if (!player.IsValid)
                {
                    return;
                }

                roundManager.ClearNextRound();

                player.SendChatAsync($"{HtmlHelper.TextWithColor("[ADMIN]", AccentColor)} " + "Следующий раунд будет выбран автоматически");
            });

            return ValueTask.CompletedTask;
        };

        menu.AddOption(option);
    }
}