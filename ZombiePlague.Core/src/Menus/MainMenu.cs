using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using Localization.Api;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Menus;

namespace ZombiePlague.Core.Menus;

internal sealed class MainMenu(
    ISwiftlyCore core,
    IMenuExtensionDispatcher extensionDispatcher,
    ZClassMenu zClassMenu,
    Func<ILocalizationApi> localization
) : DynamicOptionsMenu(core, extensionDispatcher)
{
    public override string Id => ZombiePlagueMenuIds.Main;
    
    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;
    
    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "menu",
        "main",
        "меню",
        "ьутг",
        "vty."
    ];
    
    private const string MainMenuTitle = "Menu.Main.Title";
    private const string ZClassItemTitle = "Menu.Main.Item.ZClass.Title";
    
    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization().GetForPlayer(player, MainMenuTitle) ?? MainMenuTitle)
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        var zClassButton = BuildZClassItem(player);

        options.Add(zClassButton, 1);
    }
    
    private ButtonMenuOption BuildZClassItem(IPlayer player)
    {
        var title = localization().GetForPlayer(player, ZClassItemTitle) ?? ZClassItemTitle;
        var zClassButton = new ButtonMenuOption(title);

        zClassButton.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(() => zClassMenu.Open(args.Player));
            return ValueTask.CompletedTask;
        };

        return zClassButton;
    }
}
