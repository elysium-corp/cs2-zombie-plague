using Menu.Api.Data.Contracts;
using Menu.Api.Data.Menus;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace ZombiePlague.Core.Menus.Factories;

internal sealed class MainMenuItemFactory(ISwiftlyCore core) : IMainMenuItemFactory
{
    private const string ZClassTitle = "Menu.Main.Item.ZClass.Title";

    public void OnMainMenuAddOption(IPlayer player, DynamicOptionsMenu.MenuOptionsHolder holder)
    {
        var localizer = core.Translation.GetPlayerLocalizer(player);

        var zClassButton = BuildZClassItem(localizer);

        holder.Add(zClassButton, 1);
    }

    private ButtonMenuOption BuildZClassItem(ILocalizer localizer)
    {
        var zClassButton = new ButtonMenuOption(localizer[ZClassTitle]);

        zClassButton.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(() => OpenZClassMenu(args.Player));
            return ValueTask.CompletedTask;
        };

        return zClassButton;
    }

    private static void OpenZClassMenu(IPlayer player)
    {
        var menu = ZombiePlague.MenuApi.CreateMenu<IZClassMenu>();
        menu.Open(player);
    }
}