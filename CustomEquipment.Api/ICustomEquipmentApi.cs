using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Registration;

namespace CustomEquipment.Api;

public interface ICustomEquipmentApi
{
    IEquipmentRegistrar Registrar { get; }

    IReadOnlyCollection<IWeapon> GetRegisteredWeapons();

    bool TryGetRegisteredWeapon(string internalName, [NotNullWhen(true)] out IWeapon? weapon);

    WeaponItemBase CreateWeapon(string internalName);

    TWeapon CreateWeapon<TWeapon>() where TWeapon : WeaponItemBase;

    static readonly string SharedApiKey = "CustomEquipment.Api.ICustomEquipmentApi";
}