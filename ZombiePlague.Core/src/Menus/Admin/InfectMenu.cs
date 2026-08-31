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

namespace ZombiePlague.Core.Menus.Admin;

internal sealed class InfectMenu(
    ISwiftlyCore core,
    IAdminApi adminApi,
    IPlayerManager playerManager,
    Func<ILocalizationApi> localization
) : MenuBase(core)
{
    public override string Id => "zombie_plague.admin.infect";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        var players = playerManager
            .GetAllAliveHumans()
            .OrderBy(
                target => target.Controller.PlayerName,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var target in players)
        {
            var infectTarget = new InfectTarget(
                target.PlayerID,
                target.SessionId,
                target.Controller.PlayerName
            );

            builder.AddOption(
                BuildPlayerOption(infectTarget)
            );
        }

        return builder.Build();
    }
    
    protected override bool CanOpenCore(IPlayer player)
    {
        return adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Infect);
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization().GetForPlayerOrKey(player, "ZombiePlague.Admin.Infect.Title"))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildPlayerOption(InfectTarget target)
    {
        var option = new ButtonMenuOption(target.Name);

        option.Click += (_, args) =>
        {
            var administrator = args.Player;

            Core.Scheduler.NextTick(() => InfectPlayer(administrator, target));

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private void InfectPlayer(IPlayer administrator, InfectTarget target)
    {
        if (!administrator.IsValid || !adminApi.HasPermission(administrator, ZombiePlagueAdminPermissions.Infect))
        {
            return;
        }

        var player = Core.PlayerManager.GetPlayer(target.PlayerId);

        if (player is null ||
            !player.IsValid ||
            !player.IsAlive ||
            player.SessionId != target.SessionId ||
            !playerManager.IsHuman(player))
        {
            return;
        }

        playerManager.TryInfect(player);
    }

    private readonly record struct InfectTarget(
        int PlayerId,
        ulong SessionId,
        string Name
    );
}
