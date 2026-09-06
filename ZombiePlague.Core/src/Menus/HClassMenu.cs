using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using Localization.Api;
using Metrics.Api;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Api.Menus;
using ZombiePlague.Core.Catalog;

namespace ZombiePlague.Core.Menus;

internal sealed class HClassMenu(
    ISwiftlyCore core,
    IMenuExtensionDispatcher extensionDispatcher,
    ZombieCatalogService catalog,
    IPlayerRepository playerRepository,
    IMetricsService metrics,
    Func<ILocalizationApi> localization
) : DynamicOptionsMenu(core, extensionDispatcher)
{
    public override string Id => ZombiePlagueMenuIds.HClass;

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.All;

    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "hclass",
        "humanclass",
        "рсдфыы"
    ];

    private const string HClassMenuTitle = "Menu.HClass.Title";
    private const string HClassSelected = "Menu.HClass.Selected";
    private const string HClassSelectionSuccess = "Menu.HClass.SelectionSuccess";

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization().GetForPlayer(player, HClassMenuTitle) ?? HClassMenuTitle)
            .Design.SetMenuFooterVisible(false)
            .Design.SetMenuTitleItemCountVisible()
            .Design.SetMaxVisibleItems()
            .Design.EnableAutoAdjustVisibleItems();
    }

    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        var currentZClass = playerRepository.GetHClassId(player);

        var zClasses = catalog.Current.Document.Classes
            .Where(item => item.Enabled && item.Kind == "human")
            .OrderBy(item => item.SortOrder).ThenBy(item => item.InternalName);

        foreach (var zClass in zClasses)
        {
            options.Add(BuildZClassOption(player, currentZClass, zClass));
        }
    }

    private ButtonMenuOption BuildZClassOption(IPlayer player, string currentZClass, ZombieClassDefinition zClass)
    {
        var isSelected = zClass.InternalName == currentZClass;
        var className = LocalizeClassField(player, zClass, "Name", zClass.DisplayName);
        var classDescription = LocalizeClassField(player, zClass, "Description", zClass.Description);
        var displayName = isSelected
            ? localization().GetForPlayer(
                  player,
                  HClassSelected,
                  new Dictionary<string, string> { ["class"] = className })
              ?? className
            : className;

        var option = new ButtonMenuOption
        {
            Enabled = !isSelected,
            Text = displayName,
            Comment = classDescription
        };

        option.Click += (_, args) =>
        {
            var player = args.Player;

            // Открытое меню могло пережить синхронизацию и удаление класса
            if (!catalog.Current.Document.Classes.Any(item => item.InternalName == zClass.InternalName && item.Enabled && item.Kind == "human"))
            {
                core.MenusAPI.CloseActiveMenu(player);
                return ValueTask.CompletedTask;
            }
            playerRepository.SetHClassId(player, zClass.InternalName);

            if (player.IsAuthorized && !player.IsFakeClient)
            {
                metrics.Track(
                    "class_selected",
                    player.SteamID,
                    new
                    {
                        class_id = zClass.InternalName,
                        class_name = zClass.InternalName,
                        class_type = "human"
                    }
                );
            }

            var message = localization().GetForPlayer(
                player,
                HClassSelectionSuccess,
                new Dictionary<string, string> { ["class"] = className });

            if (message is not null)
            {
                player.SendChatAsync(message);
            }

            core.MenusAPI.CloseActiveMenu(player);

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private string LocalizeClassField(
        IPlayer player,
        ZombieClassDefinition zClass,
        string field,
        string fallback)
    {
        var key = field == "Name" ? zClass.DisplayNameKey : zClass.DescriptionKey;
        return localization().GetForPlayer(player, key) ?? key;
    }
}
