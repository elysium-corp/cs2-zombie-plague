using System.Runtime.CompilerServices;
using Economy.Api;
using Economy.Api.Events;
using Localization.Api;
using Menu.Api.Data;
using Menu.Api.Data.Contracts;
using Shop.Api.Data;
using Shop.Core.Application;
using Shop.Core.Data;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;

namespace Shop.Core.Menus;

internal sealed class ShopMenu(
    ISwiftlyCore core,
    ShopSnapshotCache cache,
    ShopAccessEvaluator access,
    ShopPurchaseService purchases,
    Func<ILocalizationApi> localizationApi,
    Func<IEconomyApi> economyApi,
    Func<IZombiePlagueApi> zombiePlagueApi) : MenuBase(core), IDisposable
{
    private readonly ConditionalWeakTable<IMenuAPI, MenuMarker> _menus = new();
    private readonly Dictionary<int, long?> _screens = [];
    private readonly Dictionary<int, CancellationTokenSource> _pendingRefreshes = [];
    private readonly Dictionary<(int PlayerId, long OfferId), CancellationTokenSource> _cooldownRefreshes = [];
    private IEconomyApi? _subscribedEconomy;
    private IZombiePlagueApi? _subscribedZombiePlague;
    private bool _initialized;

    public override string Id => "shop.menu.root";

    protected override MenuTeamAccess AllowedTeams => MenuTeamAccess.Players;

    protected override IReadOnlyCollection<string> Commands { get; } =
    [
        "shop",
        "магазин",
        "ьфпфяш"
    ];

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        RebindExternalEvents();
        Core.Event.OnClientDisconnected += OnClientDisconnected;
    }

    /// <summary>
    /// Переподписывает меню после перестроения реестра shared-интерфейсов.
    /// Ссылки сохраняются явно, чтобы при hot reload отписаться именно от прежнего API.
    /// </summary>
    public void RebindExternalEvents()
    {
        if (!_initialized)
        {
            return;
        }

        var economy = economyApi();
        if (!ReferenceEquals(_subscribedEconomy, economy))
        {
            UnsubscribeEconomy();
            _subscribedEconomy = economy;
            economy.Events.Transactions.Committed.Hook(OnBalanceChanged);
            economy.Events.Transactions.Failed.Hook(OnBalanceChangeFailed);
            economy.Events.Accounts.Initialized.Hook(OnAccountInitialized);
            economy.Events.Accounts.Loaded.Hook(OnAccountLoaded);
        }

        var zombiePlague = zombiePlagueApi();
        if (!ReferenceEquals(_subscribedZombiePlague, zombiePlague))
        {
            UnsubscribeZombiePlague();
            _subscribedZombiePlague = zombiePlague;
            var players = zombiePlague.Events.Players;
            players.Infected.Hook(OnPlayerInfected);
            players.Disinfected.Hook(OnPlayerDisinfected);
            players.Humanized.Hook(OnPlayerHumanized);
            players.BecameNemesis.Hook(OnPlayerBecameNemesis);
            players.BecameSurvivor.Hook(OnPlayerBecameSurvivor);
        }
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        UnsubscribeEconomy();
        UnsubscribeZombiePlague();
        Core.Event.OnClientDisconnected -= OnClientDisconnected;

        foreach (var timer in _pendingRefreshes.Values.Concat(_cooldownRefreshes.Values))
        {
            timer.Cancel();
        }

        _pendingRefreshes.Clear();
        _cooldownRefreshes.Clear();
        _screens.Clear();
    }

    private void UnsubscribeEconomy()
    {
        if (_subscribedEconomy is not { } economy)
        {
            return;
        }

        economy.Events.Transactions.Committed.Unhook(OnBalanceChanged);
        economy.Events.Transactions.Failed.Unhook(OnBalanceChangeFailed);
        economy.Events.Accounts.Initialized.Unhook(OnAccountInitialized);
        economy.Events.Accounts.Loaded.Unhook(OnAccountLoaded);
        _subscribedEconomy = null;
    }

    private void UnsubscribeZombiePlague()
    {
        if (_subscribedZombiePlague is not { } zombiePlague)
        {
            return;
        }

        var players = zombiePlague.Events.Players;
        players.Infected.Unhook(OnPlayerInfected);
        players.Disinfected.Unhook(OnPlayerDisinfected);
        players.Humanized.Unhook(OnPlayerHumanized);
        players.BecameNemesis.Unhook(OnPlayerBecameNemesis);
        players.BecameSurvivor.Unhook(OnPlayerBecameSurvivor);
        _subscribedZombiePlague = null;
    }

    public void RefreshOpenMenus()
    {
        if (!_initialized)
        {
            return;
        }

        foreach (var player in Core.PlayerManager.GetAllPlayers())
        {
            ScheduleRefresh(player);
        }
    }

    protected override bool CanOpenCore(IPlayer player)
    {
        var snapshot = cache.Current;
        var shopType = access.GetShopType(player);
        return snapshot.Storefronts.TryGetValue(shopType, out var storefront) && storefront.Enabled;
    }

    protected override IMenuAPI Build(IPlayer player)
    {
        _screens[player.PlayerID] = null;
        return BuildRoot(player);
    }

    protected override IMenuBuilderAPI ConfigureDesign(IPlayer player, IMenuDesignAPI design)
    {
        var title = StorefrontTitle(player);
        return design
            .SetMenuTitle(WithBalance(player, title))
            .Design.SetCommentVisible()
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
    }

    private IMenuAPI BuildRoot(IPlayer player)
    {
        var snapshot = cache.Current;
        var shopType = access.GetShopType(player);
        var builder = CreateBuilder(player);
        var hasOptions = false;

        foreach (var category in snapshot.Categories
                     .Where(item => item.ShopType == shopType && item.Enabled)
                     .OrderBy(item => item.SortOrder)
                     .ThenBy(item => Localize(player, item.DisplayNameKey), StringComparer.CurrentCultureIgnoreCase))
        {
            if (!snapshot.Offers.Any(offer =>
                    offer.ShopType == shopType && offer.Enabled && offer.CategoryId == category.Id))
            {
                continue;
            }

            hasOptions = true;
            var option = new ButtonMenuOption
            {
                Text = Localize(player, category.DisplayNameKey),
                Comment = LocalizeOptional(player, category.DescriptionKey)
            };
            option.Click += (_, args) =>
            {
                Core.Scheduler.NextTickAsync(() => OpenCategory(args.Player, category.Id));
                return ValueTask.CompletedTask;
            };
            builder.AddOption(option);
        }

        foreach (var offer in SortOffers(
                     player,
                     snapshot.Offers.Where(item =>
                         item.ShopType == shopType && item.Enabled && item.CategoryId is null)))
        {
            hasOptions = true;
            builder.AddOption(BuildOfferOption(player, offer));
        }

        if (!hasOptions)
        {
            builder.AddOption(new ButtonMenuOption(Localize(player, "Shop.Menu.Empty"))
            {
                Enabled = false
            });
        }

        return Mark(builder.Build());
    }

    private void OpenCategory(IPlayer player, long categoryId)
    {
        if (!player.IsValid ||
            player.Controller.Team is not (Team.T or Team.CT) ||
            !CanOpenCore(player))
        {
            return;
        }

        var snapshot = cache.Current;
        var shopType = access.GetShopType(player);
        var category = snapshot.Categories.FirstOrDefault(item =>
            item.Id == categoryId && item.ShopType == shopType && item.Enabled);
        if (category is null)
        {
            Open(player);
            return;
        }

        _screens[player.PlayerID] = categoryId;
        var title = WithBalance(player, Localize(player, category.DisplayNameKey));
        var builder = Core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(title)
            .Design.SetCommentVisible()
            .Design.SetMenuFooterVisible(false)
            .Design.EnableAutoAdjustVisibleItems();
        var offers = SortOffers(player, snapshot.Offers.Where(item =>
            item.ShopType == shopType && item.Enabled && item.CategoryId == categoryId));

        foreach (var offer in offers)
        {
            builder.AddOption(BuildOfferOption(player, offer));
        }

        var back = new ButtonMenuOption(Localize(player, "Shop.Menu.Back"));
        back.Click += (_, args) =>
        {
            Core.Scheduler.NextTickAsync(() => Open(args.Player));
            return ValueTask.CompletedTask;
        };
        builder.AddOption(back);
        Core.MenusAPI.OpenMenuForPlayer(player, Mark(builder.Build()));
    }

    private ButtonMenuOption BuildOfferOption(IPlayer player, ShopOfferDefinition offer)
    {
        var availability = access.Evaluate(player, offer);
        if (availability.Reason == ShopAvailabilityReason.CooldownActive)
        {
            ScheduleCooldownRefresh(player, offer.Id, availability.RemainingCooldown);
        }

        var price = localizationApi().FormatForPlayer(
            player,
            "Shop.Menu.Price",
            new Dictionary<string, object?> { ["price"] = offer.Contract.Price })
            ?? "Shop.Menu.Price";
        var option = new ButtonMenuOption
        {
            Text = $"{Localize(player, offer.Contract.DisplayNameKey)} [{price}]",
            Comment = OfferComment(player, offer, availability),
            Enabled = availability.Allowed
        };
        option.Click += (_, args) =>
        {
            var buyer = args.Player;
            Core.Scheduler.NextTickAsync(() =>
            {
                if (!purchases.TryPurchase(buyer, offer.Id))
                {
                    ScheduleRefresh(buyer);
                    return;
                }

                ScheduleRefresh(buyer);
                if (offer.Contract.CooldownSeconds > 0)
                {
                    ScheduleCooldownRefresh(
                        buyer,
                        offer.Id,
                        TimeSpan.FromSeconds(offer.Contract.CooldownSeconds));
                }
            });
            return ValueTask.CompletedTask;
        };
        return option;
    }

    private IReadOnlyList<ShopOfferDefinition> SortOffers(
        IPlayer player,
        IEnumerable<ShopOfferDefinition> source)
    {
        var shopType = access.GetShopType(player);
        var mode = cache.Current.Storefronts[shopType].SortMode;
        var localized = source.Select(offer => new
        {
            Offer = offer,
            Name = Localize(player, offer.Contract.DisplayNameKey)
        });
        return mode switch
        {
            ShopSortMode.Price => localized
                .OrderBy(item => item.Offer.Contract.Price)
                .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(item => item.Offer)
                .ToArray(),
            ShopSortMode.Alphabetical => localized
                .OrderBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .ThenBy(item => item.Offer.Contract.Price)
                .Select(item => item.Offer)
                .ToArray(),
            _ => localized
                .OrderBy(item => item.Offer.Contract.SortOrder)
                .ThenBy(item => item.Name, StringComparer.CurrentCultureIgnoreCase)
                .Select(item => item.Offer)
                .ToArray()
        };
    }

    private string OfferComment(
        IPlayer player,
        ShopOfferDefinition offer,
        ShopAvailability availability)
    {
        var parts = new List<string>();
        var description = LocalizeOptional(player, offer.DescriptionKey);
        if (!string.IsNullOrWhiteSpace(description))
        {
            parts.Add(description);
        }

        if (offer.Contract.AmmoPrice is { } ammoPrice)
        {
            parts.Add(localizationApi().FormatForPlayer(
                player,
                "Shop.Menu.Ammo",
                new Dictionary<string, object?>
                {
                    ["price"] = ammoPrice,
                    ["amount"] = offer.Contract.AmmoAmount
                }) ?? "Shop.Menu.Ammo");
        }

        if (!availability.Allowed)
        {
            parts.Add(AvailabilityText(player, availability));
        }

        return string.Join(" · ", parts);
    }

    private string AvailabilityText(IPlayer player, ShopAvailability availability)
    {
        var key = ShopLocalization.AvailabilityKey(availability.Reason);
        return availability.Reason == ShopAvailabilityReason.CooldownActive
            ? localizationApi().FormatForPlayer(
                player,
                key,
                new Dictionary<string, object?>
                {
                    ["seconds"] = Math.Max(1, (int)Math.Ceiling(availability.RemainingCooldown.TotalSeconds))
                }) ?? key
            : Localize(player, key);
    }

    private string StorefrontTitle(IPlayer player)
    {
        var shopType = access.GetShopType(player);
        return cache.Current.Storefronts.TryGetValue(shopType, out var storefront)
            ? Localize(player, storefront.TitleKey)
            : Localize(player, shopType == ShopType.Human ? "Shop.Human.Title" : "Shop.Zombie.Title");
    }

    private string WithBalance(IPlayer player, string title)
    {
        var balanceValue = economyApi().GetBalance(player);
        var balance = localizationApi().FormatForPlayer(
            player,
            "Shop.Menu.Balance",
            new Dictionary<string, object?> { ["balance"] = balanceValue })
            ?? "Shop.Menu.Balance";
        return $"{title} · {balance}";
    }

    private string Localize(IPlayer player, string key) =>
        localizationApi().GetForPlayer(player, key) ?? key;

    private string LocalizeOptional(IPlayer player, string? key) =>
        string.IsNullOrWhiteSpace(key) ? string.Empty : Localize(player, key);

    private IMenuAPI Mark(IMenuAPI menu)
    {
        _menus.Add(menu, MenuMarker.Instance);
        return menu;
    }

    private void ScheduleRefresh(IPlayer player)
    {
        if (!_initialized || !player.IsValid || _pendingRefreshes.ContainsKey(player.PlayerID))
        {
            return;
        }

        var playerId = player.PlayerID;
        _pendingRefreshes[playerId] = Core.Scheduler.DelayBySeconds(0.2f, () =>
        {
            _pendingRefreshes.Remove(playerId);
            RefreshIfOpen(playerId);
        });
    }

    private void ScheduleCooldownRefresh(IPlayer player, long offerId, TimeSpan remaining)
    {
        var key = (PlayerId: player.PlayerID, OfferId: offerId);
        if (_cooldownRefreshes.Remove(key, out var previous))
        {
            previous.Cancel();
        }

        _cooldownRefreshes[key] = Core.Scheduler.DelayBySeconds(
            Math.Max(0.05f, (float)remaining.TotalSeconds + 0.05f),
            () =>
            {
                _cooldownRefreshes.Remove(key);
                RefreshIfOpen(key.PlayerId);
            });
    }

    private void RefreshIfOpen(int playerId)
    {
        if (!_initialized || Core.PlayerManager.GetPlayer(playerId) is not { IsValid: true } player)
        {
            return;
        }

        var current = Core.MenusAPI.GetCurrentMenu(player);
        if (current is null || !_menus.TryGetValue(current, out _))
        {
            return;
        }

        Core.MenusAPI.CloseActiveMenu(player);
        if (_screens.GetValueOrDefault(playerId) is { } categoryId)
        {
            OpenCategory(player, categoryId);
        }
        else
        {
            Open(player);
        }
    }

    private void RefreshForSideChange(IPlayer player)
    {
        _screens[player.PlayerID] = null;
        Core.Scheduler.NextWorldUpdate(() => RefreshIfOpen(player.PlayerID));
    }

    private void OnBalanceChanged(ref EconomyTransactionCommittedContext context) =>
        ScheduleRefresh(context.Player);

    private void OnBalanceChangeFailed(ref EconomyTransactionFailedContext context) =>
        ScheduleRefresh(context.Player);

    private void OnAccountInitialized(ref EconomyAccountInitializedContext context) =>
        ScheduleRefresh(context.Player);

    private void OnAccountLoaded(ref EconomyAccountLoadedContext context)
    {
        var steamId = context.SteamId;
        Core.Scheduler.NextTick(() =>
        {
            var player = Core.PlayerManager.GetAllPlayers().FirstOrDefault(candidate =>
                candidate.IsAuthorized && candidate.SteamID == steamId);
            if (player is not null)
            {
                ScheduleRefresh(player);
            }
        });
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context) => RefreshForSideChange(context.Player);
    private void OnPlayerDisinfected(ref PlayerDisinfectedContext context) => RefreshForSideChange(context.Player);
    private void OnPlayerHumanized(ref PlayerHumanizedContext context) => RefreshForSideChange(context.Player);
    private void OnPlayerBecameNemesis(ref PlayerBecameNemesisContext context) => RefreshForSideChange(context.Player);
    private void OnPlayerBecameSurvivor(ref PlayerBecameSurvivorContext context) => RefreshForSideChange(context.Player);

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event)
    {
        _screens.Remove(@event.PlayerId);
        if (_pendingRefreshes.Remove(@event.PlayerId, out var refresh))
        {
            refresh.Cancel();
        }

        foreach (var key in _cooldownRefreshes.Keys.Where(key => key.PlayerId == @event.PlayerId).ToArray())
        {
            _cooldownRefreshes[key].Cancel();
            _cooldownRefreshes.Remove(key);
        }
    }

    private sealed class MenuMarker
    {
        public static readonly MenuMarker Instance = new();
    }
}
