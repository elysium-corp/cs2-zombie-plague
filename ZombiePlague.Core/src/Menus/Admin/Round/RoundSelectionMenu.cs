using Admin.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Permissions;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Data.Rounds.Registrator;
using ZombiePlague.Core.Utils.Helpers;

namespace ZombiePlague.Core.Menus.Admin.Round;

internal sealed class RoundSelectionMenu(
    ISwiftlyCore core,
    IAdminApi adminApi,
    IRoundManager roundManager,
    IRoundRegistrator roundRegistrator,
    IRoundFactory roundFactory
) : MenuBase(core)
{
    private const string AccentColor = "#7DD3FC";
    private const string SuccessColor = "#86EFAC";
    private const string WarningColor = "#FDBA74";
    private const string DisabledColor = "#94A3B8";

    public override string Id => "zombie_plague.admin.round.selection";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Round);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        builder.AddOption(BuildAutomaticOption());

        foreach (var roundConfig in roundRegistrator.GetAll())
        {
            var round = roundFactory.Create(roundConfig);
            var enabled = roundConfig.Enable && roundConfig.Weight > 0;

            if (!enabled)
            {
                builder.AddOption(new ButtonMenuOption
                {
                    Text = round.Name,
                    Enabled = false
                });

                continue;
            }

            var selected = roundManager.NextRound?.Id == round.Id;
            var canStart = !roundManager.IsPreparing || round.CanStart();
            var color = canStart ? SuccessColor : WarningColor;
            var prefix = selected ? "✓ " : string.Empty;

            var target = new RoundTarget(round.Id, round.Name);

            builder.AddOption(BuildRoundOption(target, prefix, color));
        }

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        design.SetDisabledColor(DisabledColor);

        return design
            .SetMenuTitle("Следующий раунд")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildAutomaticOption()
    {
        var text = roundManager.NextRound is null
            ? "✓ Автоматически"
            : "Автоматически";

        var option = new ButtonMenuOption(HtmlHelper.TextWithColor(text, AccentColor));

        option.Click += (_, args) =>
        {
            var administrator = args.Player;

            Core.Scheduler.NextTick(() => SelectAutomatic(administrator));

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private ButtonMenuOption BuildRoundOption(RoundTarget target, string prefix, string color)
    {
        var option = new ButtonMenuOption(
            HtmlHelper.TextWithColor($"{prefix}{target.Name}", color)
        );

        option.Click += (_, args) =>
        {
            var administrator = args.Player;

            Core.Scheduler.NextTick(() => SelectRound(administrator, target));

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private void SelectAutomatic(IPlayer administrator)
    {
        if (!CanManageRound(administrator))
        {
            return;
        }

        roundManager.ClearNextRound();

        _ = administrator.SendChatAsync("Следующий раунд будет выбран автоматически.");
    }

    private void SelectRound(IPlayer administrator, RoundTarget target)
    {
        if (!CanManageRound(administrator) ||
            !roundFactory.TryCreate(target.Id, out var round))
        {
            return;
        }

        roundManager.SelectNextRound(round);

        if (roundManager.IsPreparing && !round.CanStart())
        {
            _ = administrator.SendChatAsync(
                $"Следующий раунд: {round.Name}. Сейчас условия не выполнены, они будут проверены перед запуском."
            );

            return;
        }

        _ = administrator.SendChatAsync($"Следующий раунд: {round.Name}");
    }

    private bool CanManageRound(IPlayer administrator)
    {
        return administrator.IsValid &&
               adminApi.HasPermission(administrator, ZombiePlagueAdminPermissions.Round);
    }

    private readonly record struct RoundTarget(
        string Id,
        string Name
    );
}