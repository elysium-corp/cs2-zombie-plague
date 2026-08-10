using CustomEquipment.Api.Data;

namespace Shop.Api.Data;

public sealed record ShopItem(
    string Id,
    string EquipmentItemId,
    string DisplayName,
    EquipmentCategory Category,
    int Price
);
