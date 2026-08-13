using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using CustomEquipment.Registry;

namespace CustomEquipment.Data.Catalog;

internal sealed class EquipmentShopCatalog(IItemRegistry itemRegistry) : IEquipmentShopCatalog
{
    private readonly Dictionary<string, Registration> _manualItems = new(StringComparer.OrdinalIgnoreCase);

    public IDisposable Register(IShopItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        if (string.IsNullOrWhiteSpace(item.InternalName))
        {
            throw new ArgumentException("Shop item InternalName cannot be empty!", nameof(item));
        }

        if (item.Price < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(item), item.Price, "Shop item price cannot be negative!");
        }

        if (!itemRegistry.TryGetDefinition(item.InternalName, out _))
        {
            throw new InvalidOperationException(
                $"Equipment '{item.InternalName}' must be registered before adding it to the shop!"
            );
        }

        var registration = new Registration(item);

        if (!_manualItems.TryAdd(item.InternalName, registration))
        {
            throw new InvalidOperationException($"Shop item '{item.InternalName}' is already registered!");
        }

        return new RegistrationHandle(() => Unregister(registration));
    }

    public IReadOnlyCollection<IShopItem> GetAll()
    {
        var items = itemRegistry
            .GetDefinitions()
            .OfType<IShopItem>()
            .ToDictionary(
                item => item.InternalName,
                StringComparer.OrdinalIgnoreCase
            );

        // Ручная регистрация переопределяет настройки
        // предмета из IItemRegistry.
        foreach (var registration in _manualItems.Values)
        {
            var item = registration.Item;

            if (itemRegistry.TryGetDefinition(item.InternalName, out _))
            {
                items[item.InternalName] = item;
            }
        }

        return items.Values
            .OrderBy(item => item.WeaponType)
            .ThenBy(item => item.Price)
            .ThenBy(item => item.DisplayName)
            .ToArray();
    }

    public IReadOnlyCollection<IShopItem> GetByWeaponType(WeaponType weaponType)
    {
        return GetAll()
            .Where(item => item.WeaponType == weaponType)
            .OrderBy(item => item.Price)
            .ThenBy(item => item.DisplayName)
            .ToArray();
    }

    public IReadOnlyCollection<IShopItem> GetByRarity(ItemRarity rarity)
    {
        return GetAll()
            .Where(item => item.Rarity == rarity)
            .OrderBy(item => item.WeaponType)
            .ThenBy(item => item.Price)
            .ThenBy(item => item.DisplayName)
            .ToArray();
    }

    public bool TryGet(string internalName, out IShopItem? item )
    {
        item = null;

        if (string.IsNullOrWhiteSpace(internalName))
        {
            return false;
        }

        // Ручная настройка имеет приоритет.
        if (_manualItems.TryGetValue(internalName, out var registration))
        {
            item = registration.Item;
            return true;
        }

        if (!itemRegistry.TryGetDefinition(internalName, out var definition))
        {
            return false;
        }

        item = definition as IShopItem;
        
        return item is not null;
    }

    private void Unregister(Registration registration)
    {
        var internalName = registration.Item.InternalName;

        if (!_manualItems.TryGetValue(internalName, out var current) || !ReferenceEquals(current, registration))
        {
            return;
        }

        _manualItems.Remove(internalName);
    }

    private sealed record Registration(IShopItem Item);

    private sealed class RegistrationHandle(Action unregister) : IDisposable
    {
        private Action? _unregister = unregister;

        public void Dispose()
        {
            Interlocked.Exchange(ref _unregister, null)?.Invoke();
        }
    }
}