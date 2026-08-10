using CustomEquipment.Data.Equipments.Contracts;
using CustomEquipment.Fetcher;

namespace CustomEquipment.Services;

internal sealed class ItemService(IEquipmentFetcher equipmentFetcher) : IItemService
{
    private readonly Dictionary<string, IItem> _registeredItems = new(StringComparer.OrdinalIgnoreCase);

    public void Initialize()
    {
        _registeredItems.Clear();
        
        foreach (var item in equipmentFetcher.Fetch())
        {
            if (!_registeredItems.TryAdd(item.InternalName, item))
            {
                throw new InvalidOperationException($"Equipment item id '{item.InternalName}' is registered more than once.");
            }
        }
    }

    public IReadOnlyCollection<IItem> GetAllRegisteredItems() => _registeredItems.Values;

    public bool TryGet(string itemId, out IItem item)
    {
        return _registeredItems.TryGetValue(itemId, out item!);
    }
}
