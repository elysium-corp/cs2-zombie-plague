namespace CustomEquipment.Api.Data;

public sealed record EquipmentItem(
    string Id,
    string DisplayName,
    EquipmentCategory Category,
    EquipmentSlot Slot
);
