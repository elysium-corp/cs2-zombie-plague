using Common.Di;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Exceptions;
using CustomEquipment.Utils;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Data.Equipments.Contracts;

public abstract class BaseGrenade : BaseItem, IGrenade
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
        
        if (Model.IsNotNullOrEmpty()) grenade.SetModel(Model);
        
        grenade.AttributeManager.Item.CustomName = DisplayName;
        grenade.AttributeManager.Item.CustomNameOverride = DisplayName;
        grenade.AttributeManager.Item.CustomNameUpdated();
        
        return grenade;
    }
}