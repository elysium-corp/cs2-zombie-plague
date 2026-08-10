using CustomEquipment.Api.Events;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentApi(
    IEventPublisher eventPublisher
) : ICustomEquipmentApi
{
    
}