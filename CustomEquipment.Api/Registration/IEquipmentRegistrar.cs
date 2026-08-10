using CustomEquipment.Api.Data.Contracts;

namespace CustomEquipment.Api.Registration;

public interface IEquipmentRegistrar
{
    IDisposable Register<TItem>(Func<TItem> factory) where TItem : class, IItem;
}