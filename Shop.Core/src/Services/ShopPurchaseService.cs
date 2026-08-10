using CustomEquipment.Api;
using CustomEquipment.Api.Data;
using Microsoft.Extensions.Logging;
using MoneySystem.Api;
using MSApi.Exceptions;
using Shop.Api.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Services;

internal sealed class ShopPurchaseService(
    ISwiftlyCore core,
    IShopCatalog catalog,
    IShopAccessPolicy accessPolicy,
    IMoneySystemPaymentApi moneyApi,
    ICustomEquipmentApi equipmentApi
) : IShopPurchaseService
{
    public ShopPurchaseResult TryPurchase(IPlayer player, string itemId)
    {
        ArgumentNullException.ThrowIfNull(player);

        if (!player.IsValid)
        {
            return new ShopPurchaseResult(ShopPurchaseStatus.InvalidPlayer);
        }

        if (!accessPolicy.CanUse(player))
        {
            return new ShopPurchaseResult(ShopPurchaseStatus.PlayerNotAllowed);
        }

        if (!catalog.TryGetItem(itemId, out var item) || item is null)
        {
            return new ShopPurchaseResult(ShopPurchaseStatus.ItemNotFound);
        }

        try
        {
            if (!moneyApi.TrySpendMoney(player, item.Price))
            {
                return new ShopPurchaseResult(
                    ShopPurchaseStatus.InsufficientFunds,
                    item,
                    moneyApi.GetMoney(player)
                );
            }
        }
        catch (MoneyServicesNotFoundException exception)
        {
            core.Logger.LogError(exception, "Could not access the money account for player {PlayerId}.", player.PlayerID);
            return new ShopPurchaseResult(ShopPurchaseStatus.PaymentUnavailable, item);
        }

        try
        {
            var giveResult = equipmentApi.GiveItem(
                player,
                item.EquipmentItemId,
                EquipmentGiveMode.RemoveExisting
            );

            if (giveResult == EquipmentGiveResult.Success)
            {
                return new ShopPurchaseResult(
                    ShopPurchaseStatus.Success,
                    item,
                    GetBalanceOrDefault(player)
                );
            }

            Refund(player, item);
            return new ShopPurchaseResult(
                ShopPurchaseStatus.DeliveryFailed,
                item,
                GetBalanceOrDefault(player)
            );
        }
        catch (Exception exception)
        {
            core.Logger.LogError(
                exception,
                "Could not deliver shop item {ItemId} to player {PlayerId}.",
                item.Id,
                player.PlayerID
            );

            Refund(player, item);
            return new ShopPurchaseResult(
                ShopPurchaseStatus.DeliveryFailed,
                item,
                GetBalanceOrDefault(player)
            );
        }
    }

    private void Refund(IPlayer player, ShopItem item)
    {
        try
        {
            moneyApi.GiveMoney(player, item.Price);
        }
        catch (Exception exception)
        {
            core.Logger.LogError(
                exception,
                "Could not refund {Price} for shop item {ItemId} to player {PlayerId}.",
                item.Price,
                item.Id,
                player.PlayerID
            );
        }
    }

    private int GetBalanceOrDefault(IPlayer player)
    {
        try
        {
            return moneyApi.GetMoney(player);
        }
        catch (MoneyServicesNotFoundException)
        {
            return 0;
        }
    }
}
