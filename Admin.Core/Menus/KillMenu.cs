using Admin.Api.Permissions;
using Admin.Core.Services;
using Localization.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Admin.Core.Menus;

internal sealed class KillMenu(
    ISwiftlyCore core,
    IPrivilegeService privilegeService,
    ILocalizationApi localization) : MenuBase(core)
{
    public override string Id => "admin.kill";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return privilegeService.HasPermission(player.SteamID, AdminPermissions.Kill);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        var players = Core.PlayerManager
            .GetAllValidPlayers()
            .OrderBy(target => target.Controller.PlayerName, StringComparer.OrdinalIgnoreCase);

        foreach (var target in players)
        {
            var killTarget = new KillTarget(
                target.PlayerID,
                target.SessionId,
                target.Controller.PlayerName
            );

            builder.AddOption(BuildPlayerOption(killTarget));
        }

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization.GetForPlayerOrKey(player, "Admin.Kill.Title"))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildPlayerOption(KillTarget target)
    {
        var option = new ButtonMenuOption(target.Name);

        option.Click += async (_, args) =>
        {
            await Core.Scheduler.NextTickAsync(() => { KillPlayer(args.Player, target); });
        };

        return option;
    }

    private void KillPlayer(IPlayer administrator, KillTarget target)
    {
        if (!privilegeService.HasPermission(administrator.SteamID, AdminPermissions.Kill))
        {
            return;
        }

        var player = Core.PlayerManager.GetPlayer(target.PlayerId);

        if (player is null ||
            !player.IsValid ||
            !player.IsAlive ||
            player.SessionId != target.SessionId)
        {
            return;
        }

        player.PlayerPawn?.CommitSuicide(
            explode: false,
            force: true
        );
    }

    private readonly record struct KillTarget(int PlayerId, ulong SessionId, string Name);
}
