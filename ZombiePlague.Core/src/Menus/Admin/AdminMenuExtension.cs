using Admin.Api;
using Admin.Api.Menus;
using Localization.Api;
using Menu.Api;
using Menu.Api.Extensions;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Permissions;

namespace ZombiePlague.Core.Menus.Admin;

internal sealed class AdminMenuExtension(
    ISwiftlyCore core,
    IAdminApi adminApi,
    AdminMenu adminMenu,
    Func<ILocalizationApi> localization
)
{
    private IDisposable? _subscription;

    public void Initialize(IMenuApi menuApi)
    {
        _subscription?.Dispose();

        _subscription = menuApi.Extensions.Subscribe(
            AdminMenuIds.Main,
            ExtendAdminMenu
        );
    }

    public void Uninitialize()
    {
        _subscription?.Dispose();
        _subscription = null;
    }

    private void ExtendAdminMenu(MenuExtensionContext context)
    {
        if (!HasAnyPermission(context.Player))
        {
            return;
        }

        var option = new ButtonMenuOption(
            localization().GetForPlayerOrKey(context.Player, "ZombiePlague.Admin.RootItem")
        );

        option.Click += (_, args) =>
        {
            core.Scheduler.NextTick(
                () => adminMenu.Open(args.Player)
            );

            return ValueTask.CompletedTask;
        };

        context.Options.Add(
            option,
            priority: 100
        );
    }
    
    private bool HasAnyPermission(IPlayer player)
    {
        return adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Infect) ||
               adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Disinfect) ||
               adminApi.HasPermission(player, ZombiePlagueAdminPermissions.Round);
    }
}
