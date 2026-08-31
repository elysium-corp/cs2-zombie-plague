using Admin.Api;
using Localization.Api;
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
    IRoundFactory roundFactory,
    Func<ILocalizationApi> localization
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

        builder.AddOption(BuildAutomaticOption(player));

        foreach (var roundConfig in roundRegistrator.GetAll())
        {
            var round = roundFactory.Create(roundConfig);
            var enabled = roundConfig.Enable && roundConfig.Weight > 0;

            if (!enabled)
            {
                builder.AddOption(new ButtonMenuOption
                {
                    Text = RoundName(player, round),
                    Enabled = false
                });

                continue;
            }

            var selected = roundManager.NextRound?.Id == round.Id;
            var canStart = !roundManager.IsPreparing || round.CanStart();
            var color = canStart ? SuccessColor : WarningColor;
            var prefix = selected ? "✓ " : string.Empty;

            var target = new RoundTarget(round.Id);

            builder.AddOption(BuildRoundOption(player, target, prefix, color));
        }

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        design.SetDisabledColor(DisabledColor);

        return design
            .SetMenuTitle(L(player, "ZombiePlague.Admin.Round.Selection.Title"))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildAutomaticOption(IPlayer player)
    {
        var automatic = L(player, "ZombiePlague.Admin.Round.Automatic");
        var text = roundManager.NextRound is null ? $"✓ {automatic}" : automatic;

        var option = new ButtonMenuOption(HtmlHelper.TextWithColor(text, AccentColor));

        option.Click += (_, args) =>
        {
            var administrator = args.Player;

            Core.Scheduler.NextTick(() => SelectAutomatic(administrator));

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private ButtonMenuOption BuildRoundOption(IPlayer player, RoundTarget target, string prefix, string color)
    {
        var option = new ButtonMenuOption(
            HtmlHelper.TextWithColor($"{prefix}{RoundName(player, target.Id)}", color)
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

        _ = administrator.SendChatAsync(L(administrator, "ZombiePlague.Admin.Round.Selection.AutomaticSelected"));
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
            _ = administrator.SendChatAsync(L(
                administrator,
                "ZombiePlague.Admin.Round.Selection.ConditionsPending",
                new Dictionary<string, string> { ["round"] = RoundName(administrator, round.Id) }
            ));

            return;
        }

        _ = administrator.SendChatAsync(L(
            administrator,
            "ZombiePlague.Admin.Round.Selection.Selected",
            new Dictionary<string, string> { ["round"] = RoundName(administrator, round.Id) }
        ));
    }

    private bool CanManageRound(IPlayer administrator)
    {
        return administrator.IsValid &&
               adminApi.HasPermission(administrator, ZombiePlagueAdminPermissions.Round);
    }

    private string RoundName(IPlayer player, RoundBase round) => RoundName(player, round.Id);

    private string RoundName(IPlayer player, string roundId) =>
        L(player, $"ZombiePlague.Round.{roundId}.Name");

    private string L(
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, string>? placeholders = null) =>
        localization().GetForPlayerOrKey(player, key, placeholders);

    private readonly record struct RoundTarget(string Id);
}
