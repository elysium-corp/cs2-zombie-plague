using CustomEquipment.Api.Data.Contracts;
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
            Register(item);
        }
    }

    public IReadOnlyCollection<IItem> GetAllRegisteredItems()
    {
        return _registeredItems.Values;
    }

    public bool HasRegistered<TItem>() where TItem : IItem
    {
        return _registeredItems.Values.Any(item =>
            item.GetType() == typeof(TItem)
        );
    }

    public bool TryGet(string internalName, out IItem? item)
    {
        if (string.IsNullOrWhiteSpace(internalName))
        {
            item = null;
            return false;
        }

        return _registeredItems.TryGetValue(internalName, out item);
    }

    private void Register(IItem item)
    {
        var internalName = item.InternalName;

        if (string.IsNullOrWhiteSpace(internalName))
        {
            throw new InvalidOperationException($"Equipment item '{item.GetType().FullName}' has an empty InternalName!");
        }

        if (!_registeredItems.TryAdd(internalName, item))
        {
            throw new InvalidOperationException($"Equipment item '{internalName}' is already registered!");
        }
    }
}