using CustomEquipment.Api.Data;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Menu.Api.Extensions;
using Shop.Api.Menus;
using Shop.Core.Services;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Menus;

internal sealed class ShopMenu(
    ISwiftlyCore core,
    IMenuExtensionDispatcher extensionDispatcher,
    IShopCatalog catalog,
    IShopAccessPolicy accessPolicy,
    ShopCategoryMenu categoryMenu
) : DynamicOptionsMenu(core, extensionDispatcher)
{
    public override string Id => ShopMenuIds.Main;

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.CT;

    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "shop",
        "store",
        "магазин",
        "магаз",
        "ырщз"
    ];

    protected override bool CanOpenCore(IPlayer player) => accessPolicy.CanUse(player);

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(player);

        return design
            .SetMenuTitle(localizer["Shop.Menu.Title"])
            .Design.SetMenuFooterVisible(false)
            .Design.SetMenuTitleItemCountVisible()
            .Design.EnableAutoAdjustVisibleItems();
    }

    protected override void BuildOptions(IPlayer player, MenuOptionsCollection options)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(player);
        var itemCounts = catalog
            .GetItems()
            .GroupBy(item => item.Category)
            .ToDictionary(group => group.Key, group => group.Count());

        foreach (var category in catalog.GetCategories())
        {
            var itemCount = itemCounts.GetValueOrDefault(category);
            var option = new ButtonMenuOption
            {
                Enabled = itemCount > 0,
                Text = localizer[$"Shop.Category.{category}"],
                Comment = itemCount > 0
                    ? $"{localizer["Shop.Menu.Items"]}: {itemCount}"
                    : localizer["Shop.Menu.Empty"]
            };

            option.Click += (_, args) =>
            {
                core.Scheduler.NextTickAsync(() => categoryMenu.Open(args.Player, category));
                return ValueTask.CompletedTask;
            };

            options.Add(option);
        }
    }
}
