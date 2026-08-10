using CustomEquipment.Api.Registration;

namespace CustomEquipment.Api;

internal sealed class CustomEquipmentApi(
    IEquipmentRegistrar registrar
) : ICustomEquipmentApi
{
    public IEquipmentRegistrar Registrar => registrar;
}