using System.Diagnostics;
using Shop.Api.Data;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Application;

internal sealed class ShopPurchaseCounter
{
    private readonly Dictionary<(ulong Player, long Offer), int> _roundCounts = [];
    private readonly Dictionary<(ulong Player, long Offer), int> _mapCounts = [];
    private readonly Dictionary<(ulong Player, long Offer), long> _lastPurchases = [];

    public ShopAvailability Evaluate(IPlayer player, ShopOffer offer)
    {
        var key = (PlayerKey(player), offer.Id);

        if (offer.MaxPurchasesPerRound > 0 &&
            _roundCounts.GetValueOrDefault(key) >= offer.MaxPurchasesPerRound)
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.RoundLimitReached);
        }

        if (offer.MaxPurchasesPerMap > 0 &&
            _mapCounts.GetValueOrDefault(key) >= offer.MaxPurchasesPerMap)
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.MapLimitReached);
        }

        if (offer.CooldownSeconds > 0 && _lastPurchases.TryGetValue(key, out var lastPurchase))
        {
            var elapsed = Stopwatch.GetElapsedTime(lastPurchase);
            var cooldown = TimeSpan.FromSeconds(offer.CooldownSeconds);
            if (elapsed < cooldown)
            {
                return ShopAvailability.Rejected(
                    ShopAvailabilityReason.CooldownActive,
                    cooldown - elapsed);
            }
        }

        return ShopAvailability.Available();
    }

    public void Record(IPlayer player, ShopOffer offer)
    {
        var key = (PlayerKey(player), offer.Id);
        _roundCounts[key] = _roundCounts.GetValueOrDefault(key) + 1;
        _mapCounts[key] = _mapCounts.GetValueOrDefault(key) + 1;
        _lastPurchases[key] = Stopwatch.GetTimestamp();
    }

    public void ResetRound() => _roundCounts.Clear();

    public void ResetMap()
    {
        _roundCounts.Clear();
        _mapCounts.Clear();
        _lastPurchases.Clear();
    }

    private static ulong PlayerKey(IPlayer player) =>
        player.IsAuthorized ? player.SteamID : unchecked((ulong)(uint)player.PlayerID);
}
