using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;
using ZombiePlague.Api.Menus;

namespace ZombiePlague.Core.Menus;

internal sealed class MainMenu(
    ISwiftlyCore core, 
    IMenuExtensionDispatcher extensionDispatcher,
    ZClassMenu zClassMenu
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
        var localizer = Core.Translation.GetPlayerLocalizer(player);

        return design
            .SetMenuTitle(localizer[MainMenuTitle])
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        var localizer = core.Translation.GetPlayerLocalizer(player);

        var zClassButton = BuildZClassItem(localizer);

        options.Add(zClassButton, 1);
    }
    
    private ButtonMenuOption BuildZClassItem(ILocalizer localizer)
    {
        var zClassButton = new ButtonMenuOption(localizer[ZClassItemTitle]);

        zClassButton.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(() => zClassMenu.Open(args.Player));
            return ValueTask.CompletedTask;
        };

        return zClassButton;
    }
}