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

    public virtual IReadOnlyCollection<WeaponSound> Sounds => Array.Empty<WeaponSound>();

    /// <summary>
    /// Восстанавливает подкласс и параметры оружия, сохраняя текущие патроны в обоих
    /// магазинах и резерве. Подбор и экипировка не должны пополнять боезапас.
    /// </summary>
    public override void ReapplyCustomization()
    {
        var weapon = AttachedWeapon;
        var clip1 = weapon.Clip1;
        var clip2 = weapon.Clip2;
        var reserve1 = weapon.ReserveAmmo[0];
        var reserve2 = weapon.ReserveAmmo[1];

        try
        {
            AttachBaseWeaponVData(weapon);
        }
        finally
        {
            weapon.Clip1 = clip1;
            weapon.Clip2 = clip2;
            weapon.ReserveAmmo[0] = reserve1;
            weapon.ReserveAmmo[1] = reserve2;
            weapon.Clip1Updated();
            weapon.Clip2Updated();
            weapon.ReserveAmmoUpdated();
        }
    }

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
        base.ReapplyCustomization();
        
        return weapon;
    }
}
