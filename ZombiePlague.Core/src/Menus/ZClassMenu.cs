using CustomHud.Api;
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
using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Data.Entities.Registrator;

namespace ZombiePlague.Core.Menus;

internal sealed class ZClassMenu(
    ISwiftlyCore core,
    IMenuExtensionDispatcher extensionDispatcher,
    IZClassRegistrator zClassRegistrator,
    IPlayerRepository playerRepository,
    IMetricsService metrics,
    Func<ILocalizationApi> localization,
    BannerNotificationClient? notifications = null
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
    private const string ZClassSelected = "Menu.ZClass.Selected";
    private const string ZClassSelectionSuccess = "Menu.ZClass.SelectionSuccess";

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        return design
            .SetMenuTitle(localization().GetForPlayer(player, ZClassMenuTitle) ?? ZClassMenuTitle)
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
            .Where(zClass => zClass is not ZombieClassDefinition { Kind: "nemesis" });

        foreach (var zClass in zClasses)
        {
            options.Add(BuildZClassOption(player, currentZClass, zClass));
        }
    }

    private ButtonMenuOption BuildZClassOption(IPlayer player, string currentZClass, IZClassConfig zClass)
    {
        var isSelected = zClass.InternalName == currentZClass;
        var className = LocalizeClassField(player, zClass, "Name", zClass.DisplayName);
        var classDescription = LocalizeClassField(player, zClass, "Description", zClass.Description);
        var displayName = isSelected
            ? localization().GetForPlayer(
                  player,
                  ZClassSelected,
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
            if (!zClassRegistrator.GetAllEnabled().Any(item => item.InternalName == zClass.InternalName &&
                    item is not ZombieClassDefinition { Kind: "nemesis" }))
            {
                core.MenusAPI.CloseActiveMenu(player);
                return ValueTask.CompletedTask;
            }
            playerRepository.SetZClassId(player, zClass.InternalName);

            if (player.IsAuthorized && !player.IsFakeClient)
            {
                metrics.Track(
                    "class_selected",
                    player.SteamID,
                    new
                    {
                        class_id = zClass.InternalName,
                        class_name = zClass.InternalName,
                        class_type = "zombie"
                    }
                );
            }

            notifications?.Publish(player, ZClassSelectionSuccess,
                new Dictionary<string, object?> { ["class"] = className });

            core.MenusAPI.CloseActiveMenu(player);

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private string LocalizeClassField(
        IPlayer player,
        IZClassConfig zClass,
        string field,
        string fallback)
    {
        var key = zClass is ZombieClassDefinition definition
            ? field == "Name" ? definition.DisplayNameKey : definition.DescriptionKey
            : $"ZombiePlague.ZClass.{LocalizationKey.Canonicalize(zClass.InternalName)}.{field}";
        return localization().GetForPlayer(player, key) ?? key;
    }
}
