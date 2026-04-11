using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Data.Equipments.Enums;
using CustomEquipment.Data.Equipments.Models;
using CustomEquipment.Exceptions;
using CustomEquipment.Utils;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Data.Equipments.Contracts;

internal abstract class BaseWeapon : BaseItem, IWeapon, IHasParticle
{
    public virtual CCSWeaponBase AttachedWeapon
    {
        get => AttachedEntity.As<CCSWeaponBase>() ?? throw new NotAttachedWeaponException();
        set => AttachBaseWeaponVData(value);
    }
    
    public abstract string InheritorName { get; }
    
    public abstract WeaponType WeaponType { get; }

    public virtual WeaponDamage? WeaponDamage => null;

    public virtual WeaponTiming? WeaponTiming => null;

    public virtual WeaponParticle? Particle => null;

    public virtual Ammunition? Ammunition => null; 

    [MemberNotNullWhen(true, nameof(Particle))]
    public bool HasTraceParticle()
    {
        return Particle?.Trace.IsNotNullOrEmpty() == true;
    }

    [MemberNotNullWhen(true, nameof(Particle))]
    public bool HasImpactParticle()
    {
        return Particle?.Impact.IsNotNullOrEmpty() == true;
    }
    
    [MemberNotNullWhen(true, nameof(Particle))]
    public bool HasMuzzleFlashParticle()
    {
        return Particle?.MuzzleFlash.IsNotNullOrEmpty() == true;
    }

    private CCSWeaponBase AttachBaseWeaponVData(CCSWeaponBase weapon)
    {
        AttachedEntity = weapon;
        
        weapon.ChangeSubclass(SubclassName);

        var vData = weapon.WeaponBaseVData;
        
        vData.SetAmmo(Ammunition?.Clip, Ammunition?.ReserveAmmo, weapon);
        vData.SetTiming(WeaponTiming?.CycleTime, WeaponTiming?.DeployDuration, weapon);
        vData.SetDamage(WeaponDamage?.NumBullets, WeaponDamage?.Penetration, WeaponDamage?.Range,
            WeaponDamage?.RangeModifier);
        
        if (Model.IsNotNullOrEmpty()) weapon.SetModel(Model);

        weapon.AttributeManager.Item.CustomName = DisplayName;
        weapon.AttributeManager.Item.CustomNameOverride = DisplayName;
        weapon.AttributeManager.Item.CustomNameUpdated();
        
        return weapon;
    }
}