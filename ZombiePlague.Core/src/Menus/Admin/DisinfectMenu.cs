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

internal sealed class DisinfectMenu(
    ISwiftlyCore core,
    IAdminApi adminApi,
    IPlayerManager playerManager,
    Func<ILocalizationApi> localization
) : MenuBase(core)
{
    public override string Id => "zombie_plague.admin.disinfect";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override bool CanOpenCore(IPlayer player)
    {
        return adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Disinfect);
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);

        var players = playerManager
            .GetAllAliveZombies()
            .OrderBy(
                target => target.Controller.PlayerName,
                StringComparer.OrdinalIgnoreCase
            );

        foreach (var target in players)
        {
            var disinfectTarget = new DisinfectTarget(
                target.PlayerID,
                target.SessionId,
                target.Controller.PlayerName
            );

            builder.AddOption(
                BuildPlayerOption(disinfectTarget)
            );
        }

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization().GetForPlayerOrKey(player, "ZombiePlague.Admin.Disinfect.Title"))
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private ButtonMenuOption BuildPlayerOption(DisinfectTarget target)
    {
        var option = new ButtonMenuOption(target.Name);

        option.Click += (_, args) =>
        {
            var administrator = args.Player;

            Core.Scheduler.NextTick(
                () => DisinfectPlayer(
                    administrator,
                    target
                )
            );

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private void DisinfectPlayer(IPlayer administrator, DisinfectTarget target)
    {
        if (!administrator.IsValid || !adminApi.HasPermission(administrator, ZombiePlagueAdminPermissions.Disinfect))
        {
            return;
        }

        var player = Core.PlayerManager.GetPlayer(
            target.PlayerId
        );

        if (player is null ||
            !player.IsValid ||
            !player.IsAlive ||
            player.SessionId != target.SessionId ||
            !playerManager.IsZombie(player))
        {
            return;
        }

        playerManager.TryDisinfect(player);
    }

    private readonly record struct DisinfectTarget(
        int PlayerId,
        ulong SessionId,
        string Name
    );
}
