using CustomEquipment.Api.Data.Contracts;

namespace CustomEquipment.Services;

internal interface IItemService
{
    void Initialize();

    IReadOnlyCollection<IItem> GetAllRegisteredItems();

    bool HasRegistered<TItem>() where TItem : IItem;

    bool TryGet(string internalName, out IItem? item);
}