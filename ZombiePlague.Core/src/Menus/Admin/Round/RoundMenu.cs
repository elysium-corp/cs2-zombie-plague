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
using ZombiePlague.Core.Data.Rounds;
using ZombiePlague.Core.Data.Rounds.Contracts;
using ZombiePlague.Core.Utils.Helpers;

namespace ZombiePlague.Core.Menus.Admin.Round;

internal sealed class RoundMenu(
    ISwiftlyCore core,
    IAdminApi adminApi,
    IRoundManager roundManager,
    RoundSelectionMenu roundSelectionMenu,
    Func<ILocalizationApi> localization
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

        builder.AddOption(BuildCurrentRoundOption(player));
        builder.AddOption(BuildNextRoundOption(player));
        builder.AddOption(BuildStartImmediatelyOption(player));
        builder.AddOption(BuildSelectRoundOption(player));

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(L(player, "ZombiePlague.Admin.Round.Title"))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private TextMenuOption BuildCurrentRoundOption(IPlayer player)
    {
        var roundName = roundManager.CurrentRound is { } currentRound
            ? RoundName(player, currentRound)
            : L(player, roundManager.IsPreparing
                ? "ZombiePlague.Admin.Round.State.Preparing"
                : "ZombiePlague.Admin.Round.State.None");

        var color = roundManager.CurrentRound is not null
            ? SuccessColor
            : WarningColor;

        return new TextMenuOption
        {
            Text = $"{HtmlHelper.TextWithColor(L(player, "ZombiePlague.Admin.Round.Current"), TextColor)} {HtmlHelper.TextWithColor(roundName, color)}",
            MaxWidth = 34f,
            TextSize = MenuOptionTextSize.SmallMedium
        };
    }

    private TextMenuOption BuildNextRoundOption(IPlayer player)
    {
        var roundName = roundManager.NextRound is { } nextRound
            ? RoundName(player, nextRound)
            : L(player, "ZombiePlague.Admin.Round.Automatic");

        return new TextMenuOption
        {
            Text = $"{HtmlHelper.TextWithColor(L(player, "ZombiePlague.Admin.Round.Next"), TextColor)} {HtmlHelper.TextWithColor(roundName, AccentColor)}"
        };
    }

    private ButtonMenuOption BuildStartImmediatelyOption(IPlayer player)
    {
        var option = new ButtonMenuOption
        {
            Text = HtmlHelper.TextWithColor(L(player, "ZombiePlague.Admin.Round.StartNow"), DangerColor),
            Enabled = roundManager.IsPreparing
        };

        option.Click += async (_, args) =>
        {
            var administrator = args.Player;

            await Core.Scheduler.NextTickAsync(() => StartRoundImmediately(administrator));
        };

        return option;
    }

    private ButtonMenuOption BuildSelectRoundOption(IPlayer player)
    {
        var option = new ButtonMenuOption(
            HtmlHelper.TextWithColor(L(player, "ZombiePlague.Admin.Round.SelectNext"), AccentColor)
        );

        option.Click += async (_, args) =>
        {
            await Core.Scheduler.NextTickAsync(() => roundSelectionMenu.Open(args.Player));
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
                _ = player.SendChatAsync(L(player, "ZombiePlague.Admin.Round.Started", new Dictionary<string, string>
                {
                    ["round"] = roundManager.CurrentRound is { } round
                        ? RoundName(player, round)
                        : L(player, "ZombiePlague.Admin.Round.State.Unknown")
                }));
                break;

            case RoundStartResult.NotPreparing:
                _ = player.SendChatAsync(L(player, "ZombiePlague.Admin.Round.NotPreparing"));
                break;

            case RoundStartResult.CannotStart:
                _ = player.SendChatAsync(L(player, "ZombiePlague.Admin.Round.CannotStart"));
                break;

            case RoundStartResult.Cancelled:
                _ = player.SendChatAsync(L(player, "ZombiePlague.Admin.Round.Cancelled"));
                break;
        }
    }

    private string RoundName(IPlayer player, RoundBase round) =>
        L(player, $"ZombiePlague.Round.{LocalizationKey.Canonicalize(round.Id)}.Name");

    private string L(
        IPlayer player,
        string key,
        IReadOnlyDictionary<string, string>? placeholders = null) =>
        localization().GetForPlayerOrKey(player, key, placeholders);
}
