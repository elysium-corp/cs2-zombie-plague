using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Exceptions;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Data;

public abstract class GrenadeItemBase : ItemBase, IGrenade
{
    public virtual CBaseCSGrenade AttachedGrenade
    {
        get => AttachedEntity.As<CBaseCSGrenade>() ?? throw new NotAttachedGrenadeException();
        set => AttachBaseGrenadeVData(value);
    }

    public override string SubclassName => "";

    public abstract string InheritorName { get; }
    
    public abstract WeaponType WeaponType { get; }

    public virtual void OnDetonate(IPlayer thrower, Vector position) { }
    
    private CBaseCSGrenade AttachBaseGrenadeVData(CBaseCSGrenade grenade)
    {
        AttachedEntity = grenade;
           
        if (!string.IsNullOrEmpty(Model)) grenade.SetModel(Model);
        
        grenade.AttributeManager.Item.CustomName = DisplayName;
        grenade.AttributeManager.Item.CustomNameOverride = DisplayName;
        grenade.AttributeManager.Item.CustomNameUpdated();
        
        return grenade;
    }
}