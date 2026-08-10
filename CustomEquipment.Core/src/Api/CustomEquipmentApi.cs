using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Exceptions;
using CustomEquipment.Api.Registration;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentApi(
    IItemRegistry itemRegistry,
    IEquipmentService equipmentService
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

    public WeaponItemBase? GiveWeapon(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        return equipmentService.GiveWeapon(player, internalName, action);
    }

    public TWeapon? GiveWeapon<TWeapon>(IPlayer player, GiveAction action = GiveAction.Drop) where TWeapon : WeaponItemBase
    {
        return equipmentService.GiveWeapon<TWeapon>(player, action);
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