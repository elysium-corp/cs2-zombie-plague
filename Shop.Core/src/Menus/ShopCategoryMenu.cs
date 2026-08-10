using CustomEquipment.Api.Data;
using Shop.Api.Data;
using Shop.Core.Services;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Menus;

internal sealed class ShopCategoryMenu(
    ISwiftlyCore core,
    IShopCatalog catalog,
    IShopAccessPolicy accessPolicy,
    IShopPurchaseService purchaseService
)
{
    public void Open(IPlayer player, EquipmentCategory category)
    {
        if (!accessPolicy.CanUse(player))
        {
            return;
        }

        core.MenusAPI.OpenMenuForPlayer(player, Build(player, category));
    }

    private IMenuAPI Build(IPlayer player, EquipmentCategory category)
    {
        var localizer = core.Translation.GetPlayerLocalizer(player);
        var categoryName = localizer[$"Shop.Category.{category}"];
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle($"{localizer["Shop.Menu.Title"]} / {categoryName}")
            .Design.SetMenuFooterVisible(false)
            .Design.SetMenuTitleItemCountVisible()
            .Design.EnableAutoAdjustVisibleItems();

        var items = catalog.GetItems(category);

        if (items.Count == 0)
        {
            builder.AddOption(new ButtonMenuOption
            {
                Enabled = false,
                Text = localizer["Shop.Menu.Empty"]
            });

            return builder.Build();
        }

        foreach (var item in items)
        {
            builder.AddOption(BuildItemOption(item));
        }

        return builder.Build();
    }

    private ButtonMenuOption BuildItemOption(ShopItem item)
    {
        var option = new ButtonMenuOption
        {
            Text = $"{item.DisplayName} — ${item.Price:N0}"
        };

        option.Click += (_, args) =>
        {
            var result = purchaseService.TryPurchase(args.Player, item.Id);

            SendPurchaseResult(args.Player, result);

            if (result.IsSuccess)
            {
                core.MenusAPI.CloseActiveMenu(args.Player);
            }

            return ValueTask.CompletedTask;
        };

        return option;
    }

    private void SendPurchaseResult(IPlayer player, ShopPurchaseResult result)
    {
        var localizer = core.Translation.GetPlayerLocalizer(player);

        var message = result.Status switch
        {
            ShopPurchaseStatus.Success =>
                $"{localizer["Shop.Purchase.Success"]}: {result.Item?.DisplayName}. " +
                $"{localizer["Shop.Purchase.Balance"]}: ${result.Balance:N0}",
            ShopPurchaseStatus.InsufficientFunds =>
                $"{localizer["Shop.Purchase.InsufficientFunds"]} " +
                $"{localizer["Shop.Purchase.Balance"]}: ${result.Balance:N0}",
            ShopPurchaseStatus.PlayerNotAllowed => localizer["Shop.Purchase.NotAllowed"],
            ShopPurchaseStatus.ItemNotFound => localizer["Shop.Purchase.ItemNotFound"],
            ShopPurchaseStatus.PaymentUnavailable => localizer["Shop.Purchase.PaymentUnavailable"],
            ShopPurchaseStatus.DeliveryFailed => localizer["Shop.Purchase.DeliveryFailed"],
            _ => localizer["Shop.Purchase.Failed"]
        };

        player.SendChatAsync(message);
    }
}
