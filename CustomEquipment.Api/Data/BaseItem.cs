using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Exceptions;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Data;

public abstract class BaseItem : IItem
{
    public virtual CEconEntity AttachedEntity
    {
        get => field ?? throw new NotAttachedEntityException();
        set;
    }
    
    public abstract string DisplayName { get; }

    public virtual string InternalName => ToInternalName(DisplayName);

    public abstract string SubclassName { get; }

    public abstract Slot Slot { get; }
    
    public abstract string Model { get; }
    
    public object Clone()
    {
        return MemberwiseClone();
    }
    
    private static string ToInternalName(string name)
    {
        if (string.IsNullOrEmpty(name)) return name;

        return name.ToLowerInvariant().Replace(" ", "_");
    }
}