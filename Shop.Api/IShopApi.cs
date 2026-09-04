using Shop.Api.Data;
using Shop.Api.Events;
using SwiftlyS2.Shared.Players;

namespace Shop.Api;

/// <summary>Общедоступный API магазина Zombie Plague.</summary>
public interface IShopApi
{
    /// <summary>События покупок.</summary>
    IShopEvents Events { get; }

    /// <summary>Открывает актуальный магазин для текущей стороны игрока.</summary>
    void Open(IPlayer player);

    /// <summary>Возвращает офферы текущего memory snapshot.</summary>
    IReadOnlyCollection<ShopOffer> GetOffers(ShopType shopType);

    /// <summary>Проверяет доступность оффера без обращения к базе данных.</summary>
    ShopAvailability GetAvailability(IPlayer player, long offerId);

    /// <summary>Пытается купить указанный оффер.</summary>
    bool TryPurchase(IPlayer player, long offerId);

    /// <summary>Пытается купить патроны для активного пользовательского оружия.</summary>
    bool TryPurchaseActiveWeaponAmmo(IPlayer player);

    /// <summary>Ключ общей регистрации API.</summary>
    static readonly string SharedApiKey = "Shop.Api.IShopApi";
}
