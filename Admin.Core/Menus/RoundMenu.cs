using Admin.Api.Permissions;
using Admin.Core.Services;
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
internal sealed class RoundMenu(ISwiftlyCore core, IPrivilegeService privilegeService) : MenuBase(core)
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
            .AddOption(BuildEndWarmupOption())
            .AddOption(BuildEndRoundOption())
            .Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle("Управление раундом")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildEndWarmupOption()
    {
        var option = new ButtonMenuOption("Завершить разминку")
        {
            Enabled = IsWarmupActive()
        };

        option.Validating += (_, args) =>
        {
            if (!CanManageRound(args.Player) || !IsWarmupActive())
            {
                args.Cancel = true;
            }
        };

        option.Click += (_, args) =>
        {
            EndWarmup(args.Player);

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private ButtonMenuOption BuildEndRoundOption()
    {
        var option = new ButtonMenuOption("Завершить раунд");

        option.Click += (_, args) =>
        {
            EndRound(args.Player);

            return ValueTask.CompletedTask;
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