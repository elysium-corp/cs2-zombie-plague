using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Exceptions;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Data;

public abstract class ItemBase : IItem
{
    public virtual AccessFlags AccessFlags => AccessFlags.All;
    
    public virtual CEconEntity AttachedEntity
    {
        get => field ?? throw new NotAttachedEntityException();
        set;
    }
    
    public abstract string DisplayName { get; }

    public abstract string InternalName { get; }

    public abstract string SubclassName { get; }

    public abstract Slot Slot { get; }
    
    public abstract string Model { get; }
}