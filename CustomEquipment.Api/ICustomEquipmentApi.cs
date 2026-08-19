using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Api.Registration;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api;

public interface ICustomEquipmentApi
{
    IEquipmentRegistrar Registrar { get; }

    IReadOnlyCollection<IItem> GetRegisteredItems();

    bool TryGetRegisteredItem(string internalName, [NotNullWhen(true)] out IItem? item);

    void GiveItem(IPlayer player, string internalName, GiveAction action = GiveAction.Drop);

    IItem CreateItem(string internalName);

    static readonly string SharedApiKey = "CustomEquipment.Api.ICustomEquipmentApi";
}