using System.Runtime.CompilerServices;
using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Events.Contexts.Items;
using CustomEquipment.Data.Catalog;
using CustomEquipment.Data.Shop;
using CustomEquipment.Menus.Utils;
using CustomEquipment.Services;
using CustomEquipment.Utils;
using Economy.Api;
using Localization.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;

namespace CustomEquipment.Menus;

internal sealed class EquipmentMenu(
    ISwiftlyCore core,
    IEquipmentService equipmentService,
    IEquipmentShopCatalog itemCatalog,
    EquipmentShopRuntimeCatalog shopCatalog,
    IEquipmentShopRoleResolver roleResolver,
    IEquipmentShopPurchaseLimitService purchaseLimitService,
    IEconomyApi economyApi,
    IHookPublisher hooks,
    ILocalizationApi localization,
    Func<IZombiePlagueApi> zombiePlagueApi
) : MenuBase(core), IDisposable
{
    private readonly ConditionalWeakTable<IMenuAPI, ShopMenuMarker> _shopMenus = new();
    private readonly HashSet<int> _pendingRefreshes = [];
    private bool _initialized;

    public override string Id => "equipment.menu.select-equipment";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.Players;

    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "equipment",
        "weapons",
        "shop",
        "магазин",
        "оружия",
        "пушки",
        "цуфзщты",
        "уйгшзьуте"
    ];

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Hook(OnPlayerInfected);
        playerEvents.Disinfected.Hook(OnPlayerDisinfected);
        playerEvents.Humanized.Hook(OnPlayerHumanized);
        playerEvents.BecameNemesis.Hook(OnPlayerBecameNemesis);
        playerEvents.BecameSurvivor.Hook(OnPlayerBecameSurvivor);
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        var playerEvents = zombiePlagueApi().Events.Players;
        playerEvents.Infected.Unhook(OnPlayerInfected);
        playerEvents.Disinfected.Unhook(OnPlayerDisinfected);
        playerEvents.Humanized.Unhook(OnPlayerHumanized);
        playerEvents.BecameNemesis.Unhook(OnPlayerBecameNemesis);
        playerEvents.BecameSurvivor.Unhook(OnPlayerBecameSurvivor);
        _pendingRefreshes.Clear();
    }

    protected override bool CanOpenCore(IPlayer player)
    {
        var shopType = roleResolver.GetShopType(player);
        return shopCatalog.GetSettings(shopType).Enabled;
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        var shopType = roleResolver.GetShopType(player);
        var builder = CreateBuilder(player);

        foreach (var category in shopCatalog.GetCategories(shopType))
        {
            var entries = GetEntries(player, shopType, category.Id);

            if (entries.Count == 0)
            {
                continue;
            }

            var option = new ButtonMenuOption
            {
                Text = Localize(player, category.DisplayNameKey, category.DisplayName),
                Comment = Localize(player, category.DescriptionKey, category.Description)
            };

            option.Click += (_, args) =>
            {
                core.Scheduler.NextTickAsync(() => OpenCategory(args.Player, category.Id));
                return ValueTask.CompletedTask;
            };

            builder.AddOption(option);
        }

        return Mark(builder.Build());
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        var shopType = roleResolver.GetShopType(player);
        var settings = shopCatalog.GetSettings(shopType);

        return design
            .SetMenuTitle(Localize(player, settings.DisplayNameKey, settings.DisplayName))
            .Design.SetCommentVisible()
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private void OpenCategory(IPlayer player, long categoryId)
    {
        if (!player.IsValid ||
            player.Controller.Team is not (Team.T or Team.CT))
        {
            return;
        }

        var shopType = roleResolver.GetShopType(player);
        var settings = shopCatalog.GetSettings(shopType);

        if (!settings.Enabled)
        {
            core.MenusAPI.CloseActiveMenu(player);
            return;
        }

        if (!shopCatalog.TryGetCategory(shopType, categoryId, out var category) ||
            !category.Enabled)
        {
            core.MenusAPI.CloseActiveMenu(player);
            Open(player);
            return;
        }

        var entries = GetEntries(player, shopType, category.Id);
        var builder = core.MenusAPI
            .CreateBuilder()
            .Design.SetMenuTitle(Localize(player, category.DisplayNameKey, category.DisplayName))
            .Design.SetCommentVisible()
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();

        foreach (var entry in entries)
        {
            builder.AddOption(BuildItemOption(player, entry));
        }

        var backOption = new ButtonMenuOption(
            localization.GetForPlayer(player, "Menu.Equipment.Back") ?? "Назад"
        );
        backOption.Click += (_, args) =>
        {
            core.Scheduler.NextTickAsync(() => Open(args.Player));
            return ValueTask.CompletedTask;
        };
        builder.AddOption(backOption);

        core.MenusAPI.OpenMenuForPlayer(player, Mark(builder.Build()));
    }

    private IReadOnlyList<ShopEntry> GetEntries(
        IPlayer player,
        EquipmentShopType shopType,
        long categoryId
    )
    {
        var entries = new List<ShopEntry>();

        foreach (var listing in shopCatalog.GetListings(shopType)
                     .Where(listing => listing.CategoryId == categoryId))
        {
            if (!itemCatalog.TryGet(listing.ItemInternalName, out var item) ||
                !equipmentService.CanUseItem(player, item.InternalName))
            {
                continue;
            }

            entries.Add(new ShopEntry(item, listing));
        }

        return entries
            .OrderBy(entry => entry.Listing.SortOrder)
            .ThenBy(entry => entry.Item.DisplayName)
            .ToArray();
    }

    private ButtonMenuOption BuildItemOption(IPlayer player, ShopEntry entry)
    {
        var availability = purchaseLimitService.CanPurchase(player, entry.Listing);
        var canUse = equipmentService.CanUseItem(player, entry.Item.InternalName);
        var hasMoney = economyApi.HasEnoughMoney(player, entry.Listing.Price);
        var option = new ButtonMenuOption
        {
            Text = BuildTextItem(player, entry.Item, entry.Listing.Price),
            Comment = BuildItemComment(player, entry.Listing, availability),
            Enabled = canUse && hasMoney && availability.Allowed
        };

        option.Click += (_, args) =>
        {
            var playerFromArgs = args.Player;

            if (!playerFromArgs.IsValid || !playerFromArgs.IsAlive)
            {
                return ValueTask.CompletedTask;
            }

            core.Scheduler.NextTickAsync(() => BuyItem(playerFromArgs, entry.Listing.Id));
            return ValueTask.CompletedTask;
        };

        return option;
    }

    private void BuyItem(IPlayer player, long listingId)
    {
        if (!player.IsValid ||
            !player.IsAlive ||
            player.Controller.Team is not (Team.T or Team.CT))
        {
            return;
        }

        var shopType = roleResolver.GetShopType(player);

        if (!shopCatalog.GetSettings(shopType).Enabled ||
            !shopCatalog.TryGetListing(shopType, listingId, out var listing) ||
            !listing.Enabled ||
            !shopCatalog.TryGetCategory(shopType, listing.CategoryId, out var category) ||
            !category.Enabled ||
            !itemCatalog.TryGet(listing.ItemInternalName, out var item))
        {
            return;
        }

        var preContext = new ItemPurchasingContext(player, item);

        if (!hooks.DispatchCancellable(ref preContext))
        {
            DispatchPurchaseRejected(
                preContext.Player,
                preContext.Item,
                ItemPurchaseRejectionReason.Cancelled
            );
            return;
        }

        if (!preContext.Player.IsValid ||
            !preContext.Player.IsAlive ||
            preContext.Player.Controller.Team is not (Team.T or Team.CT))
        {
            DispatchPurchaseRejected(
                preContext.Player,
                preContext.Item,
                ItemPurchaseRejectionReason.InvalidPlayer
            );
            return;
        }

        var preparedPlayer = preContext.Player;
        var preparedItem = preContext.Item;
        var preparedShopType = roleResolver.GetShopType(preparedPlayer);

        if (!shopCatalog.GetSettings(preparedShopType).Enabled ||
            !shopCatalog.TryGetListing(
                preparedShopType,
                preparedItem.InternalName,
                out var preparedListing
            ) ||
            !preparedListing.Enabled ||
            !shopCatalog.TryGetCategory(
                preparedShopType,
                preparedListing.CategoryId,
                out var preparedCategory
            ) ||
            !preparedCategory.Enabled)
        {
            SendShopUnavailable(preparedPlayer);
            DispatchPurchaseRejected(
                preparedPlayer,
                preparedItem,
                ItemPurchaseRejectionReason.ShopUnavailable
            );
            return;
        }

        if (!equipmentService.CanUseItem(preparedPlayer, preparedItem.InternalName))
        {
            preparedPlayer.SendChat(
                localization.GetForPlayer(preparedPlayer, "Equipment.Errors.RoleUnavailable")
                ?? "Этот предмет недоступен для текущей роли"
            );
            DispatchPurchaseRejected(
                preparedPlayer,
                preparedItem,
                ItemPurchaseRejectionReason.CannotUse
            );
            return;
        }

        var availability = purchaseLimitService.CanPurchase(preparedPlayer, preparedListing);

        if (!availability.Allowed)
        {
            preparedPlayer.SendChat(LimitReasonText(preparedPlayer, availability.Reason));
            DispatchPurchaseRejected(
                preparedPlayer,
                preparedItem,
                ItemPurchaseRejectionReason.LimitReached
            );
            return;
        }

        var price = preparedListing.Price;

        if (!economyApi.TrySpendMoney(preparedPlayer, price))
        {
            preparedPlayer.SendChat(
                localization.GetForPlayer(preparedPlayer, "Equipment.Errors.NotEnoughMoney")
                ?? "Недостаточно денег"
            );
            DispatchPurchaseRejected(
                preparedPlayer,
                preparedItem,
                ItemPurchaseRejectionReason.PaymentRejected
            );
            return;
        }

        var paymentContext = new ItemPaymentCommittedContext(preparedPlayer, preparedItem, price);
        hooks.Dispatch(ref paymentContext);

        if (!equipmentService.TryGiveItem(preparedPlayer, preparedItem.InternalName))
        {
            economyApi.GiveMoney(preparedPlayer, price);

            var refundContext = new ItemPaymentRefundedContext(preparedPlayer, preparedItem, price);
            hooks.Dispatch(ref refundContext);

            DispatchPurchaseRejected(
                preparedPlayer,
                preparedItem,
                ItemPurchaseRejectionReason.GrantRejected
            );
            return;
        }

        purchaseLimitService.RecordPurchase(preparedPlayer, preparedListing);

        var postContext = new ItemPurchasedContext(preparedPlayer, preparedItem);
        hooks.Dispatch(ref postContext);
    }

    private void RefreshIfShopOpen(IPlayer player)
    {
        if (!player.IsValid)
        {
            return;
        }

        var currentMenu = core.MenusAPI.GetCurrentMenu(player);

        if (currentMenu is null || !_shopMenus.TryGetValue(currentMenu, out _))
        {
            return;
        }

        core.MenusAPI.CloseActiveMenu(player);

        if (player.Controller.Team is Team.T or Team.CT &&
            shopCatalog.GetSettings(roleResolver.GetShopType(player)).Enabled)
        {
            Open(player);
        }
    }

    private void ScheduleRefresh(IPlayer player)
    {
        if (!_pendingRefreshes.Add(player.PlayerID))
        {
            return;
        }

        core.Scheduler.NextWorldUpdate(() =>
        {
            _pendingRefreshes.Remove(player.PlayerID);

            if (!_initialized)
            {
                return;
            }

            RefreshIfShopOpen(player);
        });
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context)
    {
        ScheduleRefresh(context.Player);
    }

    private void OnPlayerDisinfected(ref PlayerDisinfectedContext context)
    {
        ScheduleRefresh(context.Player);
    }

    private void OnPlayerHumanized(ref PlayerHumanizedContext context)
    {
        ScheduleRefresh(context.Player);
    }

    private void OnPlayerBecameNemesis(ref PlayerBecameNemesisContext context)
    {
        ScheduleRefresh(context.Player);
    }

    private void OnPlayerBecameSurvivor(ref PlayerBecameSurvivorContext context)
    {
        ScheduleRefresh(context.Player);
    }

    private IMenuAPI Mark(IMenuAPI menu)
    {
        _shopMenus.Add(menu, ShopMenuMarker.Instance);
        return menu;
    }

    private string BuildItemComment(
        IPlayer player,
        EquipmentShopListingDefinition listing,
        EquipmentShopPurchaseAvailability availability
    )
    {
        var description = Localize(player, listing.DescriptionKey, listing.Description);

        if (availability.Allowed)
        {
            return description;
        }

        var limitText = LimitReasonText(player, availability.Reason);
        return string.IsNullOrWhiteSpace(description)
            ? limitText
            : $"{description} · {limitText}";
    }

    private string LimitReasonText(
        IPlayer player,
        EquipmentShopPurchaseLimitReason reason
    )
    {
        var key = reason switch
        {
            EquipmentShopPurchaseLimitReason.RoundLimitReached =>
                "Equipment.Errors.ShopRoundLimit",
            EquipmentShopPurchaseLimitReason.MapLimitReached =>
                "Equipment.Errors.ShopMapLimit",
            EquipmentShopPurchaseLimitReason.ItemRoundLimitReached =>
                "Equipment.Errors.ItemRoundLimit",
            EquipmentShopPurchaseLimitReason.ItemMapLimitReached =>
                "Equipment.Errors.ItemMapLimit",
            _ => "Equipment.Errors.ShopUnavailable"
        };
        var fallback = reason switch
        {
            EquipmentShopPurchaseLimitReason.RoundLimitReached =>
                "Достигнут лимит покупок за раунд",
            EquipmentShopPurchaseLimitReason.MapLimitReached =>
                "Достигнут лимит покупок за карту",
            EquipmentShopPurchaseLimitReason.ItemRoundLimitReached =>
                "Лимит этого предмета за раунд исчерпан",
            EquipmentShopPurchaseLimitReason.ItemMapLimitReached =>
                "Лимит этого предмета за карту исчерпан",
            _ => "Магазин недоступен"
        };

        return localization.GetForPlayer(player, key) ?? fallback;
    }

    private void SendShopUnavailable(IPlayer player)
    {
        player.SendChat(
            localization.GetForPlayer(player, "Equipment.Errors.ShopUnavailable")
            ?? "Магазин недоступен"
        );
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

    private string BuildTextItem(IPlayer player, IShopItem item, int price)
    {
        var weaponColor = item.Rarity.ToColor();
        var displayNameKey = item is ILocalizedShopItem localizedItem
            ? localizedItem.DisplayNameKey
            : $"Equipment.Item.{item.InternalName.Replace(':', '.')}.Name";
        var displayName = localization.GetForPlayer(player, displayNameKey)
                          ?? item.DisplayName;
        var weaponText = HtmlHelper.TextWithColor(displayName, weaponColor);
        var priceText = HtmlHelper.TextWithColor($"{price}$", "#E0C216");

        return $"{weaponText} [{priceText}]";
    }

    private string Localize(IPlayer player, string? key, string fallback)
    {
        return string.IsNullOrWhiteSpace(key)
            ? fallback
            : localization.GetForPlayer(player, key) ?? fallback;
    }

    private sealed record ShopEntry(
        IShopItem Item,
        EquipmentShopListingDefinition Listing
    );

    private sealed class ShopMenuMarker
    {
        public static readonly ShopMenuMarker Instance = new();
    }
}
