using CustomEquipment.Api.Registration;

namespace CustomEquipment.Api;

public interface ICustomEquipmentApi
{
    IEquipmentRegistrar Registrar { get; }

    static readonly string SharedApiKey = "CustomEquipment.Api.ICustomEquipmentApi";
}