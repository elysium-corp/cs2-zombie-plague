using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Exceptions;
using CustomEquipment.Api.Registration;
using CustomEquipment.Registry;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentApi(
    IItemRegistry itemRegistry
) : ICustomEquipmentApi
{
    public IEquipmentRegistrar Registrar => itemRegistry;

    public IReadOnlyCollection<IWeapon> GetRegisteredWeapons()
    {
        return itemRegistry
            .GetDefinitions()
            .OfType<WeaponItemBase>()
            .Cast<IWeapon>()
            .ToArray();
    }

    public bool TryGetRegisteredWeapon(string internalName, [NotNullWhen(true)] out IWeapon? weapon)
    {
        weapon = null;

        if (!itemRegistry.TryGetDefinition(internalName, out var definition) || definition is not WeaponItemBase registeredWeapon)
        {
            return false;
        }

        weapon = registeredWeapon;
        return true;
    }

    public WeaponItemBase CreateWeapon(string internalName)
    {
        if (!itemRegistry.TryGetDefinition(internalName, out var definition))
        {
            throw new NotRegisteredItemException($"Equipment item '{internalName}' is not registered!");
        }

        if (definition is not WeaponItemBase)
        {
            throw new CannotCreateItemException($"Registered item '{internalName}' is not a weapon!");
        }

        return itemRegistry.Create(internalName) as WeaponItemBase
               ?? throw new CannotCreateItemException($"Factory for '{internalName}' returned an invalid weapon!");
    }

    public TWeapon CreateWeapon<TWeapon>() where TWeapon : WeaponItemBase
    {
        return itemRegistry.Create<TWeapon>();
    }
}