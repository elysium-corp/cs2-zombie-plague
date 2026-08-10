using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Registration;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api;

public interface ICustomEquipmentApi
{
    IEquipmentRegistrar Registrar { get; }

    IReadOnlyCollection<IWeapon> GetRegisteredWeapons();

    bool TryGetRegisteredWeapon(string internalName, [NotNullWhen(true)] out IWeapon? weapon);
    
    WeaponItemBase? GiveWeapon(IPlayer player, string internalName, GiveAction action = GiveAction.Drop);

    TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop) where TWeapon : WeaponItemBase;

    WeaponItemBase CreateWeapon(string internalName);

    TWeapon CreateWeapon<TWeapon>() where TWeapon : WeaponItemBase;

    static readonly string SharedApiKey = "CustomEquipment.Api.ICustomEquipmentApi";
}