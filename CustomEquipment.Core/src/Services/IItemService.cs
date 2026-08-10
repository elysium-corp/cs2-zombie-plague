using CustomEquipment.Data.Equipments.Contracts;

namespace CustomEquipment.Services;

internal interface IItemService
{
    void Initialize();
    
    IReadOnlyCollection<IItem> GetAllRegisteredItems();

    bool TryGet(string itemId, out IItem item);
}
