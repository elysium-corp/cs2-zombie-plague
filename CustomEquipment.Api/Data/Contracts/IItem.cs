using CustomEquipment.Api.Enums;

namespace CustomEquipment.Api.Data.Contracts;

public interface IItem : ICloneable
{
    string DisplayName { get; }
    
    string InternalName { get; }
    
    string SubclassName { get; }
    
    Slot Slot { get; }
}