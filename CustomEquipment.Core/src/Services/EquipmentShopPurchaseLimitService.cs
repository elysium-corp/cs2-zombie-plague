using Admin.Api;
using CustomEquipment.Data.Shop;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Services;

internal sealed class EquipmentShopPurchaseLimitService(
    ISwiftlyCore core,
    EquipmentShopRuntimeCatalog shopCatalog,
    IEquipmentShopRoleResolver roleResolver,
    IAdminApi adminApi
) : IEquipmentShopPurchaseLimitService, IDisposable
{
    private readonly Dictionary<(ulong PlayerKey, EquipmentShopType ShopType), PurchaseCounters>
        _counters = [];

    private Guid _roundStartHook = Guid.Empty;
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _initialized = true;
        _roundStartHook = core.GameEvent.HookPost<EventRoundStart>(OnRoundStart);
        core.Event.OnMapLoad += OnMapLoad;
    }

    public EquipmentShopPurchaseAvailability CanPurchase(
        IPlayer player,
        EquipmentShopListingDefinition listing
    )
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(listing);

        var shopType = roleResolver.GetShopType(player);
        var settings = shopCatalog.GetSettings(shopType);

        if (!settings.Enabled || listing.ShopType != shopType || !listing.Enabled)
        {
            return Denied(EquipmentShopPurchaseLimitReason.ShopDisabled);
        }

        var counters = GetCounters(player, shopType);
        var limits = ResolvePlayerLimits(player, settings);

        if (Reached(counters.RoundTotal, limits.MaxPurchasesPerRound))
        {
            return Denied(EquipmentShopPurchaseLimitReason.RoundLimitReached);
        }

        if (Reached(counters.MapTotal, limits.MaxPurchasesPerMap))
        {
            return Denied(EquipmentShopPurchaseLimitReason.MapLimitReached);
        }

        var roundItemCount = counters.RoundItems.GetValueOrDefault(listing.ItemInternalName);

        if (Reached(roundItemCount, listing.MaxPurchasesPerRound))
        {
            return Denied(EquipmentShopPurchaseLimitReason.ItemRoundLimitReached);
        }

        var mapItemCount = counters.MapItems.GetValueOrDefault(listing.ItemInternalName);

        return Reached(mapItemCount, listing.MaxPurchasesPerMap)
            ? Denied(EquipmentShopPurchaseLimitReason.ItemMapLimitReached)
            : new EquipmentShopPurchaseAvailability(true, EquipmentShopPurchaseLimitReason.None);
    }

    public void RecordPurchase(
        IPlayer player,
        EquipmentShopListingDefinition listing
    )
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(listing);

        var shopType = roleResolver.GetShopType(player);

        if (listing.ShopType != shopType)
        {
            return;
        }

        var counters = GetCounters(player, shopType);
        counters.RoundTotal++;
        counters.MapTotal++;
        counters.RoundItems[listing.ItemInternalName] =
            counters.RoundItems.GetValueOrDefault(listing.ItemInternalName) + 1;
        counters.MapItems[listing.ItemInternalName] =
            counters.MapItems.GetValueOrDefault(listing.ItemInternalName) + 1;
    }

    public void Dispose()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;
        core.GameEvent.Unhook(_roundStartHook);
        core.Event.OnMapLoad -= OnMapLoad;
        _roundStartHook = Guid.Empty;
        _counters.Clear();
    }

    private ResolvedLimits ResolvePlayerLimits(
        IPlayer player,
        EquipmentShopSettingsDefinition settings
    )
    {
        var roundLimit = settings.MaxPurchasesPerRound;
        var mapLimit = settings.MaxPurchasesPerMap;
        var roleLimits = shopCatalog.GetRoleLimits(settings.ShopType);

        if (roleLimits.Count == 0)
        {
            return new ResolvedLimits(roundLimit, mapLimit);
        }

        var privilegeKeys = adminApi
            .GetPlayerPrivileges(player)
            .Select(privilege => privilege.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var roleLimit in roleLimits)
        {
            if (!privilegeKeys.Contains(roleLimit.PrivilegeKey))
            {
                continue;
            }

            roundLimit = MorePermissive(roundLimit, roleLimit.MaxPurchasesPerRound);
            mapLimit = MorePermissive(mapLimit, roleLimit.MaxPurchasesPerMap);
        }

        return new ResolvedLimits(roundLimit, mapLimit);
    }

    private PurchaseCounters GetCounters(IPlayer player, EquipmentShopType shopType)
    {
        var key = (PlayerKey(player), shopType);

        if (_counters.TryGetValue(key, out var counters))
        {
            return counters;
        }

        counters = new PurchaseCounters();
        _counters[key] = counters;
        return counters;
    }

    private HookResult OnRoundStart(EventRoundStart @event)
    {
        _ = @event;

        foreach (var counters in _counters.Values)
        {
            counters.RoundTotal = 0;
            counters.RoundItems.Clear();
        }

        return HookResult.Continue;
    }

    private void OnMapLoad(IOnMapLoadEvent @event)
    {
        _ = @event;
        _counters.Clear();
    }

    private static ulong PlayerKey(IPlayer player)
    {
        return player.SteamID != 0
            ? player.SteamID
            : ulong.MaxValue - (uint)player.PlayerID;
    }

    private static bool Reached(int current, int limit)
    {
        return limit > 0 && current >= limit;
    }

    private static int MorePermissive(int current, int candidate)
    {
        return current == 0 || candidate == 0
            ? 0
            : Math.Max(current, candidate);
    }

    private static EquipmentShopPurchaseAvailability Denied(
        EquipmentShopPurchaseLimitReason reason
    )
    {
        return new EquipmentShopPurchaseAvailability(false, reason);
    }

    private sealed class PurchaseCounters
    {
        public int RoundTotal { get; set; }

        public int MapTotal { get; set; }

        public Dictionary<string, int> RoundItems { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, int> MapItems { get; } =
            new(StringComparer.OrdinalIgnoreCase);
    }

    private sealed record ResolvedLimits(
        int MaxPurchasesPerRound,
        int MaxPurchasesPerMap
    );
}
