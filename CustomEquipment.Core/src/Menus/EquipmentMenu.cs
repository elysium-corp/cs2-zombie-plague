using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Data.Catalog;
using CustomEquipment.Menus.Utils;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using Economy.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Translation;

namespace CustomEquipment.Menus;

internal sealed class EquipmentMenu(
    ISwiftlyCore core,
    IEquipmentService equipmentService,
    IEquipmentShopCatalog shopCatalog,
    IEconomyApi economyApi,
    IHookPublisher hooks
) : MenuBase(core)
{
    public override string Id => "equipment.menu.select-equipment";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.Players;

    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "equipment",
        "weapons",
        "оружия",
        "пушки",
        "цуфзщты",
        "уйгшзьуте"
    ];

    private const string EquipmentMenuTitle = "Menu.Equipment.Title";
    private const string BaseCategoryMenuPath = "Menu.Equipment.Category";

    private static readonly Category[] Categories = Enum
        .GetValues<WeaponType>()
        .Select(WeaponTypeToCategory)
        .ToArray();

    protected override IMenuAPI Build(IPlayer player)
    {
        var builder = CreateBuilder(player);
        var localizer = Core.Translation.GetPlayerLocalizer(player);

        BuildCategories(builder, localizer, player);

        return builder.Build();
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        var localizer = Core.Translation.GetPlayerLocalizer(player);

        return design.SetMenuTitle(localizer[EquipmentMenuTitle])
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private void BuildCategories(IMenuBuilderAPI builder, ILocalizer localizer, IPlayer player)
    {
        foreach (var category in Categories.OrderBy(category => category.Order))
        {
            BuildCategory(builder, localizer, player, category);
        }
    }

    private void BuildCategory(IMenuBuilderAPI builder, ILocalizer localizer, IPlayer player, Category category)
    {
        var text = localizer[category.NameLocalizationKey];
        var submenuCategory = new SubmenuMenuOption(text, BuildCategoryMenu(localizer, player, category));

        builder.AddOption(submenuCategory);
    }

    private IMenuAPI BuildCategoryMenu(ILocalizer localizer, IPlayer player, Category category)
    {
        var title = WeaponTypeToTitle(category.WeaponType, localizer);
        var menu = core.MenusAPI.CreateBuilder().Design.SetMenuTitle(title);
        var items = shopCatalog.GetByWeaponType(category.WeaponType);

        foreach (var item in items)
        {
            var price = item.Price.Item;
            var canUse = equipmentService.CanUseItem(player, item.InternalName);
            var hasMoney = economyApi.HasEnoughMoney(player, price);

            var option = new ButtonMenuOption
            {
                Text = BuildTextItem(item),
                Enabled = canUse && hasMoney
            };

            option.Click += (_, args) =>
            {
                var playerFromArgs = args.Player;

                if (!playerFromArgs.IsValid || !playerFromArgs.IsAlive)
                {
                    return ValueTask.CompletedTask;
                }

                core.Scheduler.NextTickAsync(() => BuyItem(playerFromArgs, item));

                return ValueTask.CompletedTask;
            };

            menu.AddOption(option);
        }

        return menu.Build();
    }

    private void BuyItem(IPlayer player, IShopItem item)
    {
        var preContext = new ItemPurchasingContext(player, item);

        if (!hooks.DispatchCancellable(ref preContext))
        {
            DispatchPurchaseRejected(preContext.Player, preContext.Item, ItemPurchaseRejectionReason.Cancelled);
            return;
        }

        if (!preContext.Player.IsValid || !preContext.Player.IsAlive)
        {
            DispatchPurchaseRejected(preContext.Player, preContext.Item, ItemPurchaseRejectionReason.InvalidPlayer);
            return;
        }

        var preparedPlayer = preContext.Player;
        var preparedItem = preContext.Item;

        if (!equipmentService.CanUseItem(preparedPlayer, preparedItem.InternalName))
        {
            preparedPlayer.SendChat("Невозможно купить для текущей роли!");
            DispatchPurchaseRejected(preparedPlayer, preparedItem, ItemPurchaseRejectionReason.CannotUse);
            return;
        }

        var price = preparedItem.Price.Item;

        if (!economyApi.TrySpendMoney(preparedPlayer, price))
        {
            preparedPlayer.SendChat("Недостаточно денег!");
            DispatchPurchaseRejected(preparedPlayer, preparedItem, ItemPurchaseRejectionReason.PaymentRejected);
            return;
        }

        var paymentContext = new ItemPaymentCommittedContext(preparedPlayer, preparedItem, price);
        hooks.Dispatch(ref paymentContext);

        if (!equipmentService.TryGiveItem(preparedPlayer, preparedItem.InternalName))
        {
            economyApi.GiveMoney(preparedPlayer, price);

            var refundContext = new ItemPaymentRefundedContext(preparedPlayer, preparedItem, price);
            hooks.Dispatch(ref refundContext);

            DispatchPurchaseRejected(preparedPlayer, preparedItem, ItemPurchaseRejectionReason.GrantRejected);
            return;
        }

        var postContext = new ItemPurchasedContext(preparedPlayer, preparedItem);
        hooks.Dispatch(ref postContext);
    }

    private void DispatchPurchaseRejected(
        IPlayer player,
        IShopItem item,
        ItemPurchaseRejectionReason reason
    )
    {
        var context = new ItemPurchaseRejectedContext(player, item, reason);
        hooks.Dispatch(ref context);
    }

    private static Category WeaponTypeToCategory(WeaponType weaponType, int index)
    {
        return new Category(
            NameLocalizationKey: $"{BaseCategoryMenuPath}.{weaponType}",
            WeaponType: weaponType,
            Order: index
        );
    }

    private string WeaponTypeToTitle(WeaponType weaponType, ILocalizer localizer)
    {
        return weaponType switch
        {
            WeaponType.Pistol => localizer[$"{BaseCategoryMenuPath}.{weaponType}"],
            WeaponType.SubmachineGun => localizer[$"{BaseCategoryMenuPath}.{weaponType}"],
            WeaponType.Rifle => localizer[$"{BaseCategoryMenuPath}.{weaponType}"],
            WeaponType.Shotgun => localizer[$"{BaseCategoryMenuPath}.{weaponType}"],
            WeaponType.SniperRifle => localizer[$"{BaseCategoryMenuPath}.{weaponType}"],
            WeaponType.MachineGun => localizer[$"{BaseCategoryMenuPath}.{weaponType}"],
            WeaponType.Grenade => localizer[$"{BaseCategoryMenuPath}.{weaponType}"],
            WeaponType.Equipment => localizer[$"{BaseCategoryMenuPath}.{weaponType}"]
        };
    }

    private string BuildTextItem(IShopItem item)
    {
        var weaponColor = item.Rarity.ToColor();
        var weaponText = HtmlHelper.TextWithColor(item.DisplayName, weaponColor);
        var price = $"{item.Price.Item}$";
        var priceText = $"{HtmlHelper.TextWithColor(price, "#E0C216")}";

        return $"{weaponText} [{priceText}]";
    }

    private sealed record Category(string NameLocalizationKey, WeaponType WeaponType, int Order);
}
