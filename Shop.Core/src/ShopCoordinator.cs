using Menu.Api;
using Menu.Api.Extensions;
using Shop.Core.Menus;
using Shop.Core.Services;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;

namespace Shop.Core;

internal sealed class ShopCoordinator(
    ISwiftlyCore core,
    IMenuApi menuApi,
    ShopMenu shopMenu,
    IShopAccessPolicy accessPolicy
)
{
    private IDisposable? _mainMenuSubscription;

    public void Start()
    {
        shopMenu.RegisterCommands();

        _mainMenuSubscription = menuApi.Extensions.Subscribe(
            ZombiePlague.Api.Menus.ZombiePlagueMenuIds.Main,
            ExtendMainMenu
        );
    }

    public void Stop()
    {
        shopMenu.UnregisterCommands();

        _mainMenuSubscription?.Dispose();
        _mainMenuSubscription = null;
    }

    private void ExtendMainMenu(MenuExtensionContext context)
    {
        var localizer = core.Translation.GetPlayerLocalizer(context.Player);
        var shopButton = new ButtonMenuOption
        {
            Enabled = accessPolicy.CanUse(context.Player),
            Text = localizer["Shop.MainMenu.Item"]
        };

        shopButton.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(() => shopMenu.Open(args.Player));
            return ValueTask.CompletedTask;
        };

        context.Options.Add(shopButton, 3);
    }
}
