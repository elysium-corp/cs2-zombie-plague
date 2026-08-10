namespace Shop.Api.Data;

public enum ShopPurchaseStatus
{
    Success,
    InvalidPlayer,
    PlayerNotAllowed,
    ItemNotFound,
    InsufficientFunds,
    PaymentUnavailable,
    DeliveryFailed
}
