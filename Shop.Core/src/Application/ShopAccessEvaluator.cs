using Admin.Api;
using Economy.Api;
using Shop.Api.Data;
using Shop.Core.Data;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;

namespace Shop.Core.Application;

internal sealed class ShopAccessEvaluator(
    ShopSnapshotCache cache,
    ShopPurchaseCounter counters,
    ShopProductProvider products,
    IAdminApi admin,
    Func<IEconomyApi> economyApi,
    Func<IZombiePlagueApi> zombiePlagueApi) : IShopAccessEvaluator
{
    public ShopType GetShopType(IPlayer player) => zombiePlagueApi().IsInfected(player)
        ? ShopType.Zombie
        : ShopType.Human;

    public ShopAvailability Evaluate(IPlayer player, ShopOfferDefinition offer, int? price = null)
    {
        var snapshot = cache.Current;
        if (!player.IsValid || !player.IsAlive || player.Controller.Team is not (Team.T or Team.CT))
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.InvalidPlayer);
        }

        var currentType = GetShopType(player);
        if (offer.ShopType != currentType ||
            !snapshot.Storefronts.TryGetValue(currentType, out var storefront) ||
            !storefront.Enabled)
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.TeamUnavailable);
        }

        if (!offer.Enabled ||
            offer.CategoryId is { } categoryId &&
            !snapshot.Categories.Any(category =>
                category.Id == categoryId && category.ShopType == currentType && category.Enabled))
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.Disabled);
        }

        if (!products.IsAvailable(player, offer))
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.ProductUnavailable);
        }

        if (!HasPrivileges(player, offer.Contract))
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.AccessDenied);
        }

        var effectivePrice = price ?? offer.Contract.Price;
        if (effectivePrice < 0 || !economyApi().HasEnoughMoney(player, effectivePrice))
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.InsufficientFunds);
        }

        return counters.Evaluate(player, offer.Contract);
    }

    public ShopAvailability EvaluateAmmo(IPlayer player, ShopOfferDefinition offer)
    {
        if (offer.Contract.AmmoPrice is not { } ammoPrice)
        {
            return ShopAvailability.Rejected(ShopAvailabilityReason.AmmoNotConfigured);
        }

        var baseAvailability = Evaluate(player, offer, ammoPrice);
        return baseAvailability.Reason is ShopAvailabilityReason.RoundLimitReached
            or ShopAvailabilityReason.MapLimitReached
            or ShopAvailabilityReason.CooldownActive
            ? ShopAvailability.Available()
            : baseAvailability;
    }

    private bool HasPrivileges(IPlayer player, ShopOffer offer)
    {
        var privileges = offer.RequiredPrivileges;
        return offer.AccessMode switch
        {
            ShopAccessMode.Everyone => true,
            ShopAccessMode.Any => privileges.Any(key => admin.HasPrivilege(player, key)),
            ShopAccessMode.All => privileges.All(key => admin.HasPrivilege(player, key)),
            _ => false
        };
    }
}
