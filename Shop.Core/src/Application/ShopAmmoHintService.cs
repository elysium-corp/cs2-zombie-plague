using CustomEquipment.Api;
using CustomHud.Api;
using Shop.Api.Data;
using Shop.Core.Data;
using Shop.Core.Hud;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Shop.Core.Application;

internal sealed class ShopAmmoHintService(
    ISwiftlyCore core,
    ShopSnapshotCache cache,
    IShopAccessEvaluator access,
    Func<ICustomEquipmentApi> equipmentApi,
    BannerNotificationClient notifications,
    ShopHudState hudState) : IDisposable
{
    internal const string EventKey = "Shop.Ammo.Empty";
    internal sealed record Hint(long WeaponIndex, string Product, int Price);
    private readonly Dictionary<int, (ulong SessionId, Hint Hint)> _shown = [];
    private CancellationTokenSource? _timer;
    private IDisposable? _configuration;
    private bool _mapUnloading;
    private bool _disposed;

    internal void Bind(IBannerNotificationApi? api)
    {
        _configuration?.Dispose();
        Reset();
        _configuration = api?.SubscribeConfiguration(Reset);
    }

    internal void Start()
    {
        if (_disposed || _timer is not null) return;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnClientDisconnected += OnDisconnect;
        _timer = core.Scheduler.RepeatBySeconds(.25f, Tick);
    }

    private void Tick()
    {
        if (_disposed || _mapUnloading) return;
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            if (!player.IsValid) continue;
            Update(player, Candidate(player));
        }
    }

    private Hint? Candidate(IPlayer player)
    {
        if (player.IsFakeClient || !player.IsAuthorized || !player.IsAlive
            || player.Controller.Team is not (Team.T or Team.CT)
            || hudState.IsOpen(player) || core.MenusAPI.GetCurrentMenu(player) is not null
            || access.GetShopType(player) != ShopType.Human)
            return null;

        var equipment = equipmentApi();
        if (!equipment.TryGetActiveWeapon(player, out var weapon)
            || weapon.Ammunition?.ReserveAmmo is not > 0
            || player.PlayerPawn?.WeaponServices?.ActiveWeapon.Value?.As<CCSWeaponBase>() is not { IsValid: true } active
            || !IsEmpty(active.Clip1, active.ReserveAmmo[0])
            || !equipment.CanRefillActiveWeapon(player, weapon.InternalName))
            return null;

        var offer = cache.Current.Offers.FirstOrDefault(candidate =>
            candidate.ShopType == ShopType.Human
            && candidate.Contract.ProviderKey == "custom_equipment"
            && candidate.Contract.ItemKey.Equals(weapon.InternalName, StringComparison.OrdinalIgnoreCase));
        if (offer?.Contract.AmmoPrice is not { } price || offer.Contract.AmmoAmount <= 0) return null;
        var availability = access.EvaluateAmmo(player, offer);
        if (!availability.Allowed && availability.Reason != ShopAvailabilityReason.InsufficientFunds) return null;
        return new Hint(active.Index, weapon.InternalName, price);
    }

    // Пустой магазин при доступном резерве означает перезарядку, а не покупку.
    internal static bool IsEmpty(int clip, int reserve) => clip == 0 && reserve == 0;

    internal void Update(IPlayer player, Hint? hint)
    {
        if (_shown.TryGetValue(player.PlayerID, out var previous))
        {
            if (previous.SessionId != player.SessionId)
                _shown.Remove(player.PlayerID);
            else if (hint == previous.Hint)
                return;
            else
                Hide(player);
        }
        if (hint is null) return;
        // После принятого события длительность определяет CMS; повторов каждый тик нет.
        if (notifications.Publish(player, EventKey, new Dictionary<string, object?>
            { ["product"] = hint.Product, ["product_id"] = hint.Product, ["price"] = hint.Price }))
            _shown[player.PlayerID] = (player.SessionId, hint);
    }

    internal void Hide(IPlayer player)
    {
        if (_shown.Remove(player.PlayerID, out var previous) && previous.SessionId == player.SessionId)
            notifications.Hide(player, EventKey);
    }

    internal void Reset()
    {
        notifications.Clear(EventKey);
        _shown.Clear();
    }

    private void OnMapUnload(IOnMapUnloadEvent args) { _mapUnloading = true; Reset(); }
    private void OnMapLoad(IOnMapLoadEvent args) => _mapUnloading = false;
    private void OnDisconnect(IOnClientDisconnectedEvent args) => _shown.Remove(args.PlayerId);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _configuration?.Dispose();
        _configuration = null;
        core.Event.OnMapUnload -= OnMapUnload;
        core.Event.OnMapLoad -= OnMapLoad;
        core.Event.OnClientDisconnected -= OnDisconnect;
        Reset();
    }
}
