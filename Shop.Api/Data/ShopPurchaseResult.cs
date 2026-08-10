namespace Shop.Api.Data;

public readonly record struct ShopPurchaseResult(
    ShopPurchaseStatus Status,
    ShopItem? Item = null,
    int Balance = 0
)
{
    public bool IsSuccess => Status == ShopPurchaseStatus.Success;
}
