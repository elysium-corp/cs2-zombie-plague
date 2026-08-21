using Admin.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Permissions;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds;
using ZombiePlague.Core.Utils.Helpers;

namespace ZombiePlague.Core.Menus.Admin.Round;

internal sealed class RoundMenu(
    ISwiftlyCore core,
    IAdminApi adminApi,
    IRoundManager roundManager,
    RoundSelectionMenu roundSelectionMenu
) : MenuBase(core)
{
    private const string AccentColor = "#7DD3FC";
    private const string SuccessColor = "#86EFAC";
    private const string WarningColor = "#FDBA74";
    private const string DangerColor = "#FCA5A5";
    private const string TextColor = "#E2E8F0";

    public override string Id => "zombie_plague.admin.round";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Round);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        builder.AddOption(BuildCurrentRoundOption());
        builder.AddOption(BuildNextRoundOption());
        builder.AddOption(BuildStartImmediatelyOption());
        builder.AddOption(BuildSelectRoundOption());

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle("Управление раундами")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private TextMenuOption BuildCurrentRoundOption()
    {
        var roundName = roundManager.CurrentRound?.Name ??
                        (roundManager.IsPreparing ? "Подготовка" : "Нет");

        var color = roundManager.CurrentRound is not null
            ? SuccessColor
            : WarningColor;

        return new TextMenuOption
        {
            Text = $"{HtmlHelper.TextWithColor("Текущий:", TextColor)} {HtmlHelper.TextWithColor(roundName, color)}",
            MaxWidth = 34f,
            TextSize = MenuOptionTextSize.SmallMedium
        };
    }

    private TextMenuOption BuildNextRoundOption()
    {
        var roundName = roundManager.NextRound?.Name ?? "Автоматически";

        return new TextMenuOption
        {
            Text = $"{HtmlHelper.TextWithColor("Следующий:", TextColor)} {HtmlHelper.TextWithColor(roundName, AccentColor)}"
        };
    }

    private ButtonMenuOption BuildStartImmediatelyOption()
    {
        var option = new ButtonMenuOption
        {
            Text = HtmlHelper.TextWithColor("⚡ Запустить немедленно", DangerColor),
            Enabled = roundManager.IsPreparing
        };

        option.Click += async (_, args) =>
        {
            var administrator = args.Player;

            await Core.Scheduler.NextTickAsync(() => StartRoundImmediately(administrator));
        };

        return option;
    }

    private ButtonMenuOption BuildSelectRoundOption()
    {
        var option = new ButtonMenuOption(
            HtmlHelper.TextWithColor("Выбрать следующий раунд", AccentColor)
        );

        option.Click += async (_, args) =>
        {
            await Core.Scheduler.NextTickAsync(() => { roundSelectionMenu.Open(args.Player); });
        };

        return option;
    }

    private void StartRoundImmediately(IPlayer administrator)
    {
        if (!administrator.IsValid ||
            !adminApi.HasPermission(administrator, ZombiePlagueAdminPermissions.Round))
        {
            return;
        }

        var result = roundManager.TryStartRandomRound();

        SendRoundStartResult(administrator, result);
    }

    private void SendRoundStartResult(IPlayer player, RoundStartResult result)
    {
        switch (result)
        {
            case RoundStartResult.Started:
                _ = player.SendChatAsync($"Запущен раунд: {roundManager.CurrentRound?.Name ?? "Неизвестно"}");
                break;

            case RoundStartResult.NotPreparing:
                _ = player.SendChatAsync("Невозможно запустить раунд: подготовка уже завершена");
                break;

            case RoundStartResult.CannotStart:
                _ = player.SendChatAsync("Не удалось подобрать раунд для запуска");
                break;

            case RoundStartResult.Cancelled:
                _ = player.SendChatAsync("Запуск раунда был отменён");
                break;
        }
    }
}