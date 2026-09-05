using Common.Hooks;
using Common.Hooks.Abstractions;
using CustomEquipment.Api;
using CustomEquipment.Api.Data.Models;
using Economy.Api;
using Localization.Api;
using Microsoft.Extensions.Logging;
using Shop.Api.Data;
using Shop.Api.Events;
using Shop.Core.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Application;

internal sealed class ShopPurchaseService(
    ISwiftlyCore core,
    ShopSnapshotCache cache,
    IShopAccessEvaluator access,
    ShopProductProvider products,
    ShopPurchaseCounter counters,
    Func<IEconomyApi> economyApi,
    Func<ICustomEquipmentApi> equipmentApi,
    IHookPublisher hooks,
    Func<ILocalizationApi> localizationApi,
    ILogger<ShopPurchaseService> logger,
    IShopSoundFeedback soundFeedback)
{
    public IReadOnlyCollection<ShopOffer> GetOffers(ShopType shopType) => cache.Current.Offers
        .Where(offer => offer.ShopType == shopType)
        .Select(offer => offer.Contract)
        .ToArray();

    public ShopAvailability GetAvailability(IPlayer player, long offerId)
    {
        var offer = FindOffer(offerId);
        return offer is null
            ? ShopAvailability.Rejected(ShopAvailabilityReason.Disabled)
            : access.Evaluate(player, offer);
    }

    public bool TryPurchase(IPlayer player, long offerId)
    {
        var offer = FindOffer(offerId);
        if (offer is null)
        {
            Reject(player, null, ShopAvailabilityReason.Disabled);
            return false;
        }

        var initialAvailability = access.Evaluate(player, offer);
        if (!initialAvailability.Allowed)
        {
            Reject(player, offer.Contract, initialAvailability.Reason, initialAvailability.RemainingCooldown);
            return false;
        }

        var context = new ShopPurchasingContext(player, offer.Contract, offer.Contract.Price);
        if (!hooks.DispatchCancellable(ref context))
        {
            Reject(context.Player, offer.Contract, ShopAvailabilityReason.Cancelled);
            return false;
        }

        var availability = access.Evaluate(context.Player, offer, context.Price);
        if (!availability.Allowed)
        {
            Reject(context.Player, offer.Contract, availability.Reason, availability.RemainingCooldown);
            return false;
        }

        var charge = Charge(context.Player, offer.Contract, context.Price);
        if (!charge.Success)
        {
            Reject(context.Player, offer.Contract, charge.FailureReason);
            return false;
        }

        var granted = false;
        try
        {
            granted = products.TryGrant(context.Player, offer);
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "[Shop] Выдача оффера {OfferId} ({ProviderKey}:{ItemKey}) завершилась ошибкой.",
                offer.Id,
                offer.Contract.ProviderKey,
                offer.Contract.ItemKey);
        }

        if (!granted)
        {
            var reason = TryRefund(context.Player, offer.Contract, charge)
                ? ShopAvailabilityReason.GrantRejected
                : ShopAvailabilityReason.RefundFailed;
            Reject(context.Player, offer.Contract, reason);
            return false;
        }

        counters.Record(context.Player, offer.Contract);
        var purchased = new ShopPurchasedContext(
            context.Player,
            offer.Contract,
            charge.DeductedAmount);
        hooks.Dispatch(ref purchased);
        return true;
    }

    public bool TryPurchaseActiveWeaponAmmo(IPlayer player)
    {
        if (!player.IsValid || !player.IsAlive ||
            core.MenusAPI.GetCurrentMenu(player) is not null ||
            !equipmentApi().TryGetActiveWeapon(player, out var weapon))
        {
            return false;
        }

        var shopType = access.GetShopType(player);
        var offer = cache.Current.Offers.FirstOrDefault(candidate =>
            candidate.ShopType == shopType &&
            candidate.Contract.ProviderKey == "custom_equipment" &&
            candidate.Contract.ItemKey.Equals(weapon.InternalName, StringComparison.OrdinalIgnoreCase));
        if (offer?.Contract.AmmoPrice is not { } ammoPrice)
        {
            return false;
        }

        var availability = access.EvaluateAmmo(player, offer);
        if (!availability.Allowed && availability.Reason != ShopAvailabilityReason.InsufficientFunds)
        {
            Reject(player, offer.Contract, availability.Reason, availability.RemainingCooldown, notifyPlayer: false);
            return false;
        }

        if (!equipmentApi().CanRefillActiveWeapon(player, offer.Contract.ItemKey))
        {
            Reject(player, offer.Contract, ShopAvailabilityReason.AmmoFull, notifyPlayer: false);
            soundFeedback.AmmoFull(player);
            return false;
        }

        // Полный резерв подтверждаем звуком даже при пустом балансе, до попытки списания.
        if (!availability.Allowed)
        {
            Reject(player, offer.Contract, availability.Reason, availability.RemainingCooldown, notifyPlayer: false);
            return false;
        }

        var charge = Charge(player, offer.Contract, ammoPrice);
        if (!charge.Success)
        {
            Reject(player, offer.Contract, charge.FailureReason, notifyPlayer: false);
            return false;
        }

        AmmoRefillResult result;
        var refilled = false;
        try
        {
            refilled = equipmentApi().TryRefillActiveWeapon(
                player,
                offer.Contract.ItemKey,
                offer.Contract.AmmoAmount,
                out result);
        }
        catch (Exception exception)
        {
            result = default;
            logger.LogError(
                exception,
                "[Shop] Пополнение патронов оффера {OfferId} ({ItemKey}) завершилось ошибкой.",
                offer.Id,
                offer.Contract.ItemKey);
        }

        if (!refilled)
        {
            var reason = TryRefund(player, offer.Contract, charge)
                ? ShopAvailabilityReason.AmmoFull
                : ShopAvailabilityReason.RefundFailed;
            Reject(player, offer.Contract, reason, notifyPlayer: false);
            if (reason == ShopAvailabilityReason.AmmoFull)
            {
                soundFeedback.AmmoFull(player);
            }
            return false;
        }

        var purchased = new ShopAmmoPurchasedContext(
            player,
            offer.Contract,
            charge.DeductedAmount,
            result.AddedAmount,
            result.ReserveAmmo);
        hooks.Dispatch(ref purchased);
        soundFeedback.AmmoPurchased(player);
        return true;
    }

    private ShopOfferDefinition? FindOffer(long offerId) =>
        cache.Current.Offers.FirstOrDefault(offer => offer.Id == offerId);

    private ChargeAttempt Charge(IPlayer player, ShopOffer offer, int amount)
    {
        var economy = economyApi();
        var balanceBefore = economy.GetBalance(player);

        try
        {
            if (!economy.TrySpendMoney(player, amount))
            {
                return ChargeAttempt.Rejected(balanceBefore);
            }

            var balanceAfter = economy.GetBalance(player);
            return ChargeAttempt.Committed(balanceBefore, Math.Max(0, balanceBefore - balanceAfter));
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "[Shop] Списание {Amount} для оффера {OfferId} завершилось ошибкой.",
                amount,
                offer.Id);

            var balanceAfter = economy.GetBalance(player);
            var deductedAmount = Math.Max(0, balanceBefore - balanceAfter);
            if (deductedAmount == 0 || TryRefund(
                    player,
                    offer,
                    ChargeAttempt.Committed(balanceBefore, deductedAmount)))
            {
                return ChargeAttempt.Rejected(balanceBefore);
            }

            return ChargeAttempt.RefundRejected(balanceBefore, deductedAmount);
        }
    }

    private bool TryRefund(IPlayer player, ShopOffer offer, ChargeAttempt charge)
    {
        if (charge.DeductedAmount == 0)
        {
            return true;
        }

        try
        {
            var economy = economyApi();
            economy.GiveMoney(player, charge.DeductedAmount);
            var currentBalance = economy.GetBalance(player);
            if (currentBalance >= charge.BalanceBefore)
            {
                return true;
            }

            logger.LogCritical(
                "[Shop] Возврат для оффера {OfferId} был отклонён: ожидался баланс не ниже " +
                "{ExpectedBalance}, фактический баланс {CurrentBalance}.",
                offer.Id,
                charge.BalanceBefore,
                currentBalance);
            return false;
        }
        catch (Exception exception)
        {
            logger.LogCritical(
                exception,
                "[Shop] Не удалось вернуть {Amount} после ошибки оффера {OfferId}.",
                charge.DeductedAmount,
                offer.Id);
            return false;
        }
    }

    private void Reject(
        IPlayer player,
        ShopOffer? offer,
        ShopAvailabilityReason reason,
        TimeSpan cooldown = default,
        bool notifyPlayer = true)
    {
        var context = new ShopPurchaseRejectedContext(player, offer, reason);
        hooks.Dispatch(ref context);

        if (!notifyPlayer || !player.IsValid)
        {
            return;
        }

        var key = ShopLocalization.AvailabilityKey(reason);
        var text = reason == ShopAvailabilityReason.CooldownActive
            ? localizationApi().FormatForPlayer(
                player,
                key,
                new Dictionary<string, object?>
                {
                    ["seconds"] = Math.Max(1, (int)Math.Ceiling(cooldown.TotalSeconds))
                })
            : localizationApi().GetForPlayer(player, key);
        player.SendChat(text ?? key);
    }

    private readonly record struct ChargeAttempt(
        bool Success,
        ShopAvailabilityReason FailureReason,
        int BalanceBefore,
        int DeductedAmount)
    {
        public static ChargeAttempt Committed(int balanceBefore, int deductedAmount) =>
            new(true, ShopAvailabilityReason.Available, balanceBefore, deductedAmount);

        public static ChargeAttempt Rejected(int balanceBefore) =>
            new(false, ShopAvailabilityReason.PaymentRejected, balanceBefore, 0);

        public static ChargeAttempt RefundRejected(int balanceBefore, int deductedAmount) =>
            new(false, ShopAvailabilityReason.RefundFailed, balanceBefore, deductedAmount);
    }
}
