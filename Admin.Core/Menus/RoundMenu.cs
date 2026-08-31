using Admin.Api.Permissions;
using Admin.Core.Services;
using Localization.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Menus;

/// <summary>
/// Меню управления текущим раундом и разминкой.
/// </summary>
internal sealed class RoundMenu(
    ISwiftlyCore core,
    IPrivilegeService privilegeService,
    ILocalizationApi localization) : MenuBase(core)
{
    public override string Id => "admin.round";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return privilegeService.HasPermission(player.SteamID, AdminPermissions.Round);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        return CreateBuilder(player)
            .AddOption(BuildEndWarmupOption(player))
            .AddOption(BuildEndRoundOption(player))
            .AddOption(BuildRestartGameOption(player))
            .Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization.GetForPlayerOrKey(player, "Admin.Round.Title"))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildEndWarmupOption(IPlayer player)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "Admin.Round.EndWarmup"))
        {
            Enabled = IsWarmupActive()
        };

        option.Click += async (_, args) =>
        {
            await Core.Scheduler.NextTickAsync(() => EndWarmup(args.Player));
        };

        return option;
    }

    private ButtonMenuOption BuildEndRoundOption(IPlayer player)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "Admin.Round.EndRound"));

        option.Click += async (_, args) =>
        {
            await Core.Scheduler.NextTickAsync(() => EndRound(args.Player));
        };

        return option;
    }
    
    private ButtonMenuOption BuildRestartGameOption(IPlayer player)
    {
        var option = new ButtonMenuOption(localization.GetForPlayerOrKey(player, "Admin.Round.Restart"));

        option.Click += async (_, args) =>
        {
            await Core.Scheduler.NextTickAsync(() => RestartGame(args.Player));
        };

        return option;
    }

    private void EndWarmup(IPlayer administrator)
    {
        if (!CanManageRound(administrator) || !IsWarmupActive())
        {
            return;
        }

        Core.Engine.ExecuteCommand("mp_warmup_end");
    }

    private void EndRound(IPlayer administrator)
    {
        if (!CanManageRound(administrator))
        {
            return;
        }

        var gameRules = Core.EntitySystem.GetGameRules();

        if (gameRules is null)
        {
            return;
        }

        gameRules.TerminateRound(
            RoundEndReason.RoundDraw,
            delay: 0f
        );
    }
    
    private void RestartGame(IPlayer administrator)
    {
        if (!CanManageRound(administrator))
        {
            return;
        }

        Core.Engine.ExecuteCommand("mp_restartgame 1");
    }

    private bool IsWarmupActive()
    {
        var gameRules = Core.EntitySystem.GetGameRules();

        return gameRules is not null && gameRules.WarmupPeriod;
    }

    private bool CanManageRound(IPlayer player)
    {
        return player.IsValid && privilegeService.HasPermission(player.SteamID, AdminPermissions.Round);
    }
}
