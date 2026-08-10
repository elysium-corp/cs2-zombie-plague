# Shop

`Shop.Core` is the implementation plugin. `Shop.Api` contains the shared public
contract that other plugins can consume without referencing implementation
types.

The shop discovers items through `ICustomEquipmentApi`, so a new registered
CustomEquipment item automatically appears in the category declared by that
item. Categories without products remain visible but disabled.

## Configuration

The plugin creates `shop.json` with category defaults. A product can be hidden,
renamed or assigned its own price by its CustomEquipment id:

```json
{
  "Shop": {
    "Enabled": true,
    "Prices": {
      "Pistol": 1500,
      "SubmachineGun": 3500,
      "Rifle": 5000,
      "Shotgun": 4000,
      "SniperRifle": 6500,
      "MachineGun": 6000,
      "Grenade": 1500,
      "Equipment": 3000
    },
    "Items": {
      "m4a1-s_x3": {
        "Enabled": true,
        "Price": 7500,
        "DisplayName": "M4A1-S X3"
      },
      "lasermine": {
        "Enabled": true,
        "Price": 4000
      }
    }
  }
}
```

## Shared API

```csharp
var shopApi = interfaceManager.GetSharedInterface<IShopApi>(IShopApi.SharedApiKey);

var result = shopApi.TryPurchase(player, "m4a1-s_x3");
if (result.IsSuccess)
{
    // The payment was completed and CustomEquipment delivered the item.
}
```

Players can open the shop with `!shop`, `!store`, `!магазин` or from the main
Zombie Plague menu. Only living, non-infected CT players can purchase items.
