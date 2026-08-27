using CustomEquipment.Api.Events;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentEvents(
    CustomEquipmentPreEvents pre,
    CustomEquipmentPostEvents post) : ICustomEquipmentEvents
{
    public ICustomEquipmentPreEvents Pre => pre;

    public ICustomEquipmentPostEvents Post => post;
}
