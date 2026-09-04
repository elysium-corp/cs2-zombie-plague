using Shop.Api.Data;

namespace Shop.Core.Application;

internal static class ShopLocalization
{
    public static string AvailabilityKey(ShopAvailabilityReason reason) => reason switch
    {
        ShopAvailabilityReason.ProductUnavailable => "Shop.Errors.ProductUnavailable",
        ShopAvailabilityReason.TeamUnavailable => "Shop.Errors.TeamUnavailable",
        ShopAvailabilityReason.AccessDenied => "Shop.Errors.AccessDenied",
        ShopAvailabilityReason.InsufficientFunds => "Shop.Errors.NotEnoughMoney",
        ShopAvailabilityReason.RoundLimitReached => "Shop.Errors.RoundLimit",
        ShopAvailabilityReason.MapLimitReached => "Shop.Errors.MapLimit",
        ShopAvailabilityReason.CooldownActive => "Shop.Errors.Cooldown",
        ShopAvailabilityReason.InvalidPlayer => "Shop.Errors.InvalidPlayer",
        ShopAvailabilityReason.Cancelled => "Shop.Errors.Cancelled",
        ShopAvailabilityReason.PaymentRejected => "Shop.Errors.PaymentRejected",
        ShopAvailabilityReason.GrantRejected => "Shop.Errors.GrantRejected",
        ShopAvailabilityReason.RefundFailed => "Shop.Errors.RefundFailed",
        ShopAvailabilityReason.AmmoNotConfigured => "Shop.Errors.AmmoNotConfigured",
        ShopAvailabilityReason.AmmoFull => "Shop.Errors.AmmoFull",
        _ => "Shop.Errors.Unavailable"
    };
}
