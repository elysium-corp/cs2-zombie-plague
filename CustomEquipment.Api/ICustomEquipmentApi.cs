using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api;

public interface ICustomEquipmentApi
{
    IReadOnlyCollection<EquipmentItem> GetItems();

    bool TryGetItem(string itemId, [NotNullWhen(true)] out EquipmentItem? item);

    EquipmentGiveResult GiveItem(
        IPlayer player,
        string itemId,
        EquipmentGiveMode mode = EquipmentGiveMode.DropExisting
    );

    static readonly string SharedApiKey = "CustomEquipment.Api.ICustomEquipmentApi";
}
