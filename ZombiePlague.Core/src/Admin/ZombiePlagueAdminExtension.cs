using Admin.Api.Menus;
using Menu.Api;
using Menu.Api.Extensions;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;

namespace ZombiePlague.Core.Admin;

internal sealed class ZombiePlagueAdminExtension(ISwiftlyCore core, ZombiePlagueAdminMenu adminMenu)
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
        var option = new ButtonMenuOption("[Zombie Mode] Админка");

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
}