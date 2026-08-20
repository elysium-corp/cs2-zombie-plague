using Admin.Api.Permissions;
using Admin.Core.Services;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Admin.Core.Menus;

internal sealed class KickMenu(ISwiftlyCore core, IPrivilegeService privilegeService) : MenuBase(core)
{
    public override string Id => "admin.kick";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return privilegeService.HasPermission(player.SteamID, AdminPermissions.Kick);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        var players = Core.PlayerManager
            .GetAllValidPlayers()
            .Where(target => !target.IsFakeClient && target.PlayerID != player.PlayerID)
            .OrderBy(target => target.Controller.PlayerName, StringComparer.OrdinalIgnoreCase);

        foreach (var target in players)
        {
            builder.AddOption(BuildPlayerOption(target));
        }

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle("Кикнуть игрока")
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildPlayerOption(IPlayer target)
    {
        var option = new ButtonMenuOption(target.Controller.PlayerName);

        option.Click += (_, args) => KickPlayer(args.Player, target);

        return option;
    }

    private ValueTask KickPlayer(IPlayer player, IPlayer target)
    {
        if (!privilegeService.HasPermission(player.SteamID, AdminPermissions.Kick))
        {
            return ValueTask.CompletedTask;
        }

        if (!target.IsValid || !player.IsFakeClient || target.PlayerID == player.PlayerID)
        {
            return ValueTask.CompletedTask;
        }

        return new ValueTask(
            target.KickAsync(
                reason: $"Вы были исключены администратором: {player.Name}", 
                gameReason: ENetworkDisconnectionReason.NETWORK_DISCONNECT_KICKED
            )
        );
    }
}