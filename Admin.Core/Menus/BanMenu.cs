using Admin.Api.Permissions;
using Admin.Core.Services;
using Common.Database.Tasks;
using Localization.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Menus;

internal sealed class BanMenu(
    ISwiftlyCore core, IPrivilegeService privilegeService, IBanService banService,
    IBanEnforcementService banEnforcementService, DatabaseTaskTracker databaseTasks,
    ILocalizationApi localization
) : MenuBase(core)
{
    private static readonly string[] PredefinedReasonKeys =
    [
        "Admin.Ban.Reason.Cheating",
        "Admin.Ban.Reason.Toxicity",
        "Admin.Ban.Reason.Spam",
        "Admin.Ban.Reason.GameplayInterference",
        "Admin.Ban.Reason.Evasion",
        "Admin.Ban.Reason.Advertising",
        "Admin.Ban.Reason.RulesViolation"
    ];

    public override string Id => "admin.ban";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return privilegeService.HasPermission(player.SteamID, AdminPermissions.Ban);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        var players = Core.PlayerManager
            .GetAllValidPlayers()
            .Where(target => target.IsAuthorized && target.SteamID != 0 && target.PlayerID != player.PlayerID)
            .OrderBy(target => target.Controller.PlayerName, StringComparer.OrdinalIgnoreCase);

        foreach (var target in players)
        {
            var banTarget = new BanTarget(
                target.PlayerID,
                target.SteamID,
                target.Controller.PlayerName
            );

            builder.AddOption(BuildPlayerOption(banTarget));
        }

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization.GetForPlayerOrKey(player, "Admin.Ban.Title"))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildPlayerOption(BanTarget target)
    {
        var option = new ButtonMenuOption(target.Name);

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => OpenDurationMenu(args.Player, target));

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private void OpenDurationMenu(IPlayer player, BanTarget target)
    {
        if (!CanBan(player))
        {
            return;
        }

        var builder = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(localization.GetForPlayerOrKey(
                player,
                "Admin.Ban.DurationTitle",
                new Dictionary<string, string> { ["player"] = target.Name }))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();

        AddDurationOption(builder, player, target, "Admin.Ban.Duration.30Minutes", TimeSpan.FromMinutes(30));
        AddDurationOption(builder, player, target, "Admin.Ban.Duration.1Hour", TimeSpan.FromHours(1));
        AddDurationOption(builder, player, target, "Admin.Ban.Duration.6Hours", TimeSpan.FromHours(6));
        AddDurationOption(builder, player, target, "Admin.Ban.Duration.1Day", TimeSpan.FromDays(1));
        AddDurationOption(builder, player, target, "Admin.Ban.Duration.7Days", TimeSpan.FromDays(7));
        AddDurationOption(builder, player, target, "Admin.Ban.Duration.30Days", TimeSpan.FromDays(30));
        AddDurationOption(builder, player, target, "Admin.Ban.Duration.Permanent", null);

        Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
    }

    private void AddDurationOption(
        IMenuBuilderAPI builder,
        IPlayer player,
        BanTarget target,
        string titleKey,
        TimeSpan? duration)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, titleKey));

        option.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => OpenReasonModeMenu(args.Player, target, duration));

            return ValueTask.CompletedTask;
        };

        builder.AddOption(option);
    }

    private void OpenReasonModeMenu(IPlayer player, BanTarget target, TimeSpan? duration)
    {
        if (!CanBan(player))
        {
            return;
        }

        var builder = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(ReasonTitle(player, target))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();

        var predefinedOption = new ButtonMenuOption(
            localization.GetForPlayerOrKey(player, "Admin.Ban.ChoosePredefinedReason"));

        predefinedOption.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => OpenPredefinedReasonMenu(args.Player, target, duration));

            return ValueTask.CompletedTask;
        };

        var customOption = new ButtonMenuOption(
            localization.GetForPlayerOrKey(player, "Admin.Ban.EnterCustomReason"));

        customOption.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => OpenCustomReasonMenu(args.Player, target, duration));

            return ValueTask.CompletedTask;
        };

        builder.AddOption(predefinedOption);
        builder.AddOption(customOption);

        Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
    }

    private void OpenPredefinedReasonMenu(IPlayer player, BanTarget target, TimeSpan? duration)
    {
        if (!CanBan(player))
        {
            return;
        }

        var builder = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(ReasonTitle(player, target))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();

        foreach (var reasonKey in PredefinedReasonKeys)
        {
            var reason = localization.GetForPlayerOrKey(player, reasonKey);
            var option = new ButtonMenuOption(reason);

            option.Click += async (_, args) =>
            {
                await Core.Scheduler.NextTickAsync(() =>
                {
                    ApplyBan(args.Player, target, duration, reason);
                });
            };

            builder.AddOption(option);
        }

        Core.MenusAPI.OpenMenuForPlayer(player, builder.Build());
    }

    private void OpenCustomReasonMenu(IPlayer player, BanTarget target, TimeSpan? duration)
    {
        if (!CanBan(player))
        {
            return;
        }

        var input = new InputMenuOption(
            text: localization.GetForPlayerOrKey(player, "Admin.Ban.CustomReasonInput"),
            maxLength: 256,
            validator: value => !string.IsNullOrWhiteSpace(value),
            defaultValue: string.Empty,
            hintMessage: localization.GetForPlayerOrKey(player, "Admin.Ban.CustomReasonHint")
        );

        input.ValueChanged += (_, args) => ApplyBan(args.Player, target, duration, args.NewValue);

        var menu = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(ReasonTitle(player, target))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems()
            .AddOption(input)
            .Build();

        Core.MenusAPI.OpenMenuForPlayer(player, menu);
    }

    private string ReasonTitle(IPlayer player, BanTarget target)
    {
        return localization.GetForPlayerOrKey(
            player,
            "Admin.Ban.ReasonTitle",
            new Dictionary<string, string> { ["player"] = target.Name });
    }

    private void ApplyBan(IPlayer player, BanTarget target, TimeSpan? duration, string reason)
    {
        if (!CanBan(player) || string.IsNullOrWhiteSpace(reason))
        {
            return;
        }

        var administratorSteamId = player.SteamID;

        Core.MenusAPI.CloseActiveMenu(player);

        databaseTasks.Run(
            async () =>
            {
                await banService
                    .BanAsync(target.SteamId, administratorSteamId, duration, reason)
                    .ConfigureAwait(false);

                Core.Scheduler.NextTick(() => EnforceBan(target));
            },
            $"Ban player {target.SteamId}"
        );
    }

    private void EnforceBan(BanTarget target)
    {
        var player = Core.PlayerManager.GetPlayer(target.PlayerId);

        if (player is null || player.IsFakeClient || player.SteamID != target.SteamId)
        {
            return;
        }

        banEnforcementService.Check(player);
    }

    private bool CanBan(IPlayer player)
    {
        return player.IsValid && player.IsAuthorized && privilegeService.HasPermission(player.SteamID, AdminPermissions.Ban);
    }

    private readonly record struct BanTarget(int PlayerId, ulong SteamId, string Name);
}
