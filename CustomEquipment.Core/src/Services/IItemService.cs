using CustomEquipment.Data.Equipments.Contracts;

namespace CustomEquipment.Services;

internal interface IItemService
{
    void Initialize();
    
    HashSet<IItem> GetAllRegisteredItems();

    public bool HasRegistered<TItem>() where TItem : IItem;
}