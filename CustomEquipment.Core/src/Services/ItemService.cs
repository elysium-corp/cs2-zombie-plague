using CustomEquipment.Api.Data;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Fetcher;

namespace CustomEquipment.Services;

internal sealed class ItemService(IEquipmentFetcher equipmentFetcher) : IItemService
{
    private readonly HashSet<IItem> _registeredItems = [];

    public void Initialize()
    {
        _registeredItems.Clear();
        
        var registeredItems = equipmentFetcher.Fetch();
        
        _registeredItems.UnionWith(registeredItems);
    }

    public HashSet<IItem> GetAllRegisteredItems() => _registeredItems;
    
    public bool HasRegistered<TItem>() where TItem : IItem
    {
        return GetAllRegisteredItems().Any(type => type.GetType() == typeof(TItem));
    }
}