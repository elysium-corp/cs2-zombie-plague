using CustomEquipment.Data.Equipments.Enums;

namespace CustomEquipment.Data.Equipments.Contracts;

public interface IItem : ICloneable
{
    string DisplayName { get; }
    
    string InternalName { get; }
    
    string SubclassName { get; }
    
    Slot Slot { get; }
}