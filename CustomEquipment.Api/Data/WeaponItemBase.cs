using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Exceptions;
using CustomEquipment.Data.Equipments.Models;
using CustomEquipment.Utils;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Api.Data;

public abstract class WeaponItemBase : ItemBase, IWeapon, IHasParticle
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
        return !string.IsNullOrEmpty(Particle?.Trace);
    }

    [MemberNotNullWhen(true, nameof(Particle))]
    public bool HasImpactParticle()
    {
        return !string.IsNullOrEmpty(Particle?.Impact);
    }
    
    [MemberNotNullWhen(true, nameof(Particle))]
    public bool HasMuzzleFlashParticle()
    {
        return !string.IsNullOrEmpty(Particle?.MuzzleFlash);
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
        
        if (!string.IsNullOrEmpty(Model)) weapon.SetModel(Model);

        weapon.AttributeManager.Item.CustomName = DisplayName;
        weapon.AttributeManager.Item.CustomNameOverride = DisplayName;
        weapon.AttributeManager.Item.CustomNameUpdated();
        
        return weapon;
    }
}