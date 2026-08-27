using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Api.Menus;
using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Data.Entities.Registrator;

namespace ZombiePlague.Core.Menus;

internal sealed class ZClassMenu(
    ISwiftlyCore core,
    IMenuExtensionDispatcher extensionDispatcher,
    IZClassRegistrator zClassRegistrator,
    IPlayerRepository playerRepository,
    ZClassCardMenu zClassCardMenu
) : DynamicOptionsMenu(core, extensionDispatcher)
{
    public override string Id => ZombiePlagueMenuIds.ZClass;

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "class",
        "zclass",
        "ясдфыы",
        "сдфыы"
    ];

    private const string ZClassMenuTitle = "Menu.ZClass.Title";

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        var locale = Core.Translation.GetPlayerLocalizer(player);

        return design
            .SetMenuTitle(locale[ZClassMenuTitle])
            .Design.SetMenuFooterVisible(false)
            .Design.SetMenuTitleItemCountVisible()
            .Design.SetMaxVisibleItems()
            .Design.EnableAutoAdjustVisibleItems();
    }

    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        var currentZClass = playerRepository.GetZClassId(player);

        var zClasses = zClassRegistrator
            .GetAllEnabled()
            .Where(zClass => zClass is not ZombieNemesis);

        foreach (var zClass in zClasses)
        {
            options.Add(BuildZClassOption(currentZClass, zClass));
        }
    }

    private ButtonMenuOption BuildZClassOption(string currentZClass, IZClassConfig zClass)
    {
        var isSelected = zClass.InternalName == currentZClass;

        var option = new ButtonMenuOption
        {
            Text = isSelected
                ? $"{zClass.DisplayName} [выбран]"
                : zClass.DisplayName,

            Comment = zClass.Description
        };

        option.Click += (_, args) =>
        {
            var player = args.Player;

            core.Scheduler.NextWorldUpdate(() => zClassCardMenu.Open(player, zClass));

            return ValueTask.CompletedTask;
        };

        return option;
    }
}
