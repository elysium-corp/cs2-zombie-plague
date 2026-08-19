using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Registration;
using CustomEquipment.Registry;
using CustomEquipment.Services;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentApi(IItemRegistry itemRegistry, IEquipmentService equipmentService) : ICustomEquipmentApi
{
    public IEquipmentRegistrar Registrar => itemRegistry;

    public IReadOnlyCollection<IItem> GetRegisteredItems()
    {
        return itemRegistry.GetDefinitions();
    }

    public bool TryGetRegisteredItem(string internalName, [NotNullWhen(true)] out IItem? item)
    {
        return itemRegistry.TryGetDefinition(internalName, out item);
    }

    public IItem CreateItem(string internalName)
    {
        return itemRegistry.Create(internalName);
    }

    public void GiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop)
    {
        equipmentService.GiveItem(player, internalName, action);
    }

    public bool CanUseItem(IPlayer player, string internalName)
    {
        return equipmentService.CanUseItem(player, internalName);
    }
}