using Common.Di;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Data.Equipments.Weapons.Equipments;

public sealed class LaserMine : EquipmentItemBase, IShopItem
{
    public override string InheritorName => WeaponName.Healthshot;

    public override AccessFlags AccessFlags => AccessFlags.Human;

    public override string DisplayName => "Laser Mine";

    public override string InternalName => "custom_equipment:laser_mine";

    public override string SubclassName => "";

    public override Slot Slot => Slot.Equipment;

    public override string Model => "models/lasermine.vmdl";

    public override WeaponType WeaponType => WeaponType.Equipment;

    public Price Price => new()
    {
        Item = 100
    };

    public ItemRarity Rarity => ItemRarity.Rare;

    public override void OnPurchase(IPlayer player)
    {
        
    }
}
