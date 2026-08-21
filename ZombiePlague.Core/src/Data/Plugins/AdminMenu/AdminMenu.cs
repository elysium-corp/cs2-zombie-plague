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
            (roundManager.IsPreparing ? "Подготовка" : "Нет");

        var currentRoundColor =
            roundManager.CurrentRound is not null
                ? SuccessColor
                : WarningColor;

        var nextRoundName = roundManager.NextRound?.Name ?? "Автоматически";

        var currentOption = new TextMenuOption
        {
            Text = $"{HtmlHelper.TextWithColor("Текущий:", TextColor)} " + HtmlHelper.TextWithColor(currentRoundName, currentRoundColor),

            MaxWidth = 34f,
            TextSize = MenuOptionTextSize.SmallMedium
        };

        menu.AddOption(currentOption);

        var nextOption = new TextMenuOption
        {
            Text = $"{HtmlHelper.TextWithColor("Следующий:", TextColor)} " + HtmlHelper.TextWithColor(nextRoundName, AccentColor)
        };

        menu.AddOption(nextOption);

        AddStartImmediatelyOption(menu);

        menu.AddOption(
            new SubmenuMenuOption(
                HtmlHelper.TextWithColor("Выбрать следующий раунд", AccentColor),
                SetNextRound
            )
        );

        return menu.Build();
    }

    private IMenuAPI SetNextRound()
    {
        var menu = core.MenusAPI.CreateBuilder();

        menu.Design.SetMenuTitle("Следующий раунд");
        menu.Design.SetDisabledColor(DisabledColor);

        AddAutomaticRoundOption(menu);

        foreach (var roundConfig in roundRegistrator.GetAll())
        {
            var enabled = roundConfig is
            {
                Enable: true,
                Weight: > 0
            };

            var round = roundFactory.Create(roundConfig);

            var isSelected = roundManager.NextRound?.Id == round.Id;

            if (!enabled)
            {
                var disabledOption = new ButtonMenuOption
                {
                    Text = round.Name,
                    Enabled = false
                };

                menu.AddOption(disabledOption);

                continue;
            }

            var canStart = !roundManager.IsPreparing || round.CanStart();

            var color = canStart ? SuccessColor : WarningColor;

            var prefix = isSelected ? "✓ " : string.Empty;

            var roundOption = new ButtonMenuOption
            {
                Text = HtmlHelper.TextWithColor($"{prefix}{round.Name}", color),
                Enabled = true
            };

            roundOption.Click += (_, args) =>
            {
                var player = args.Player;

                core.Scheduler.NextTick(() =>
                {
                    if (!player.IsValid)
                    {
                        return;
                    }

                    roundManager.SelectNextRound(round);

                    if (roundManager.IsPreparing && !round.CanStart())
                    {
                        player.SendChatAsync($"Следующий раунд: {round.Name}. " + "Сейчас условия не выполнены, они будут проверены перед запуском.");

                        return;
                    }

                    player.SendChatAsync($"Следующий раунд: {round.Name}");
                });

                return ValueTask.CompletedTask;
            };

            menu.AddOption(roundOption);
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

                player.SendChatAsync($"Запущен раунд: {startedRound?.Name ?? "Неизвестно"}");

                break;
            }

            case RoundStartResult.NotPreparing:
            {
                player.SendChatAsync("Невозможно запустить раунд: подготовка уже завершена");

                break;
            }

            case RoundStartResult.CannotStart:
            {
                player.SendChatAsync("Не удалось подобрать раунд для запуска");

                break;
            }

            case RoundStartResult.Cancelled:
            {
                player.SendChatAsync("Запуск раунда был отменён");

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
        var text = roundManager.NextRound is null
            ? "✓ Автоматически"
            : "Автоматически";

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

                player.SendChatAsync("Следующий раунд будет выбран автоматически.");
            });

            return ValueTask.CompletedTask;
        };

        menu.AddOption(option);
    }
}