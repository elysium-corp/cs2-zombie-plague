using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Data.Models;
using CustomEquipment.Api.Enums;
using CustomEquipment.Data.Equipments.Models;

namespace CustomEquipment.Data.DatabaseWeapons;

internal sealed class DatabaseWeaponItem(DatabaseWeaponDefinition definition)
    : WeaponItemBase, ILocalizedShopItem, IHasRarity
{
    public override string InheritorName => definition.InheritorName;

    public override AccessFlags AccessFlags => definition.AccessFlags;

    public override string DisplayName => definition.DisplayName;

    public string DisplayNameKey => definition.DisplayNameKey;

    public override string InternalName => definition.InternalName;

    public override string SubclassName => definition.SubclassName;

    public override Slot Slot => definition.Slot;

    public override WeaponType WeaponType => definition.WeaponType;

    public override string Model => definition.Model;

    public override WeaponDamage? WeaponDamage => definition.WeaponDamage;

    public override WeaponTiming? WeaponTiming => definition.WeaponTiming;

    public override WeaponParticle? Particle => definition.Particle;

    public override Ammunition? Ammunition => definition.Ammunition;

    public override IReadOnlyCollection<WeaponSound> Sounds => definition.Sounds;

    public ItemRarity Rarity => definition.Rarity;

    internal DatabaseWeaponItem CreateInstance() => new(definition);
}

internal sealed record DatabaseWeaponDefinition(
    string InheritorName,
    AccessFlags AccessFlags,
    string DisplayName,
    string DisplayNameKey,
    string InternalName,
    string SubclassName,
    Slot Slot,
    WeaponType WeaponType,
    string Model,
    WeaponDamage? WeaponDamage,
    WeaponTiming? WeaponTiming,
    WeaponParticle? Particle,
    Ammunition? Ammunition,
    IReadOnlyCollection<WeaponSound> Sounds,
    ItemRarity Rarity
);
