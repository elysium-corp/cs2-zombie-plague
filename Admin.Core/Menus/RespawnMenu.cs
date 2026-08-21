using Admin.Api.Permissions;
using Admin.Core.Services;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Menus;

internal sealed class RespawnMenu(ISwiftlyCore core, IPrivilegeService privilegeService) : MenuBase(core)
{
    public override string Id => "admin.respawn";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return privilegeService.HasPermission(player.SteamID, AdminPermissions.Respawn);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        var players = Core.PlayerManager
            .GetAllValidPlayers()
            .Where(target => !target.IsAlive)
            .OrderBy(target => target.Controller.PlayerName, StringComparer.OrdinalIgnoreCase);

        foreach (var target in players)
        {
            var respawnTarget = new RespawnTarget(
                target.PlayerID,
                target.SessionId,
                target.Controller.PlayerName
            );

            builder.AddOption(BuildPlayerOption(respawnTarget));
        }

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle("Возродить игрока")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildPlayerOption(RespawnTarget target)
    {
        var option = new ButtonMenuOption(target.Name);

        option.Click += async (_, args) =>
        {
            await Core.Scheduler.NextTickAsync(() => { RespawnPlayer(args.Player, target); });
        };

        return option;
    }

    private void RespawnPlayer(IPlayer administrator, RespawnTarget target)
    {
        if (!privilegeService.HasPermission(administrator.SteamID, AdminPermissions.Respawn))
        {
            return;
        }

        var player = Core.PlayerManager.GetPlayer(target.PlayerId);

        if (player is null ||
            !player.IsValid ||
            player.IsAlive ||
            player.SessionId != target.SessionId)
        {
            return;
        }

        player.Respawn();
    }

    private readonly record struct RespawnTarget(int PlayerId, ulong SessionId, string Name);
}