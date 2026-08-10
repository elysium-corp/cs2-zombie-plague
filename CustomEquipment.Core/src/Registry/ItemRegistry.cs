using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Exceptions;
using CustomEquipment.Fetcher;

namespace CustomEquipment.Registry;

internal sealed class ItemRegistry(IEquipmentFetcher equipmentFetcher) : IItemRegistry
{
    private readonly Dictionary<string, Registration> _registrationsById = new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<Type, Registration> _registrationsByType = [];

    public void Initialize()
    {
        _registrationsById.Clear();
        _registrationsByType.Clear();

        foreach (var definition in equipmentFetcher.Fetch())
        {
            RegisterBuiltIn(definition);
        }
    }

    public IReadOnlyCollection<IItem> GetDefinitions()
    {
        return _registrationsById.Values
            .Select(registration => registration.Definition)
            .ToArray();
    }

    public bool TryGetDefinition(string internalName, [NotNullWhen(true)] out IItem? definition
    )
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(internalName))
        {
            return false;
        }

        if (!_registrationsById.TryGetValue(internalName, out var registration))
        {
            return false;
        }

        definition = registration.Definition;
        return true;
    }

    public TItem Create<TItem>() where TItem : class, IItem
    {
        var itemType = typeof(TItem);

        if (!_registrationsByType.TryGetValue(itemType, out var registration))
        {
            throw new NotRegisteredItemException($"Equipment item '{itemType.FullName}' is not registered!");
        }

        return Create(registration) as TItem
            ?? throw new CannotCreateItemException($"Registered factory returned an invalid '{itemType.FullName}'!");
    }

    public IItem Create(string internalName)
    {
        if (string.IsNullOrWhiteSpace(internalName))
        {
            throw new NotRegisteredItemException("Equipment item ID cannot be empty!");
        }

        if (!_registrationsById.TryGetValue(internalName, out var registration))
        {
            throw new NotRegisteredItemException($"Equipment item '{internalName}' is not registered!");
        }

        return Create(registration);
    }
    
    public IDisposable Register<TItem>(Func<TItem> factory) where TItem : class, IItem
    {
        ArgumentNullException.ThrowIfNull(factory);

        var definition = factory()
                         ?? throw new CannotCreateItemException($"Factory for '{typeof(TItem).FullName}' returned null!");

        if (definition.GetType() != typeof(TItem))
        {
            throw new CannotCreateItemException(
                $"Factory registered as '{typeof(TItem).FullName}' " + $"returned '{definition.GetType().FullName}'!"
            );
        }

        var registration = Register(
            definition,
            factory: () => factory() ?? throw new CannotCreateItemException($"Factory for '{typeof(TItem).FullName}' returned null!")
        );

        return new RegistrationHandle(
            () => Unregister(registration)
        );
    }

    private void RegisterBuiltIn(IItem definition)
    {
        var itemType = definition.GetType();

        Register(
            definition,
            factory: () => Activate(itemType)
        );
    }

    private Registration Register(IItem definition, Func<IItem> factory)
    {
        var internalName = definition.InternalName;
        var itemType = definition.GetType();

        if (string.IsNullOrWhiteSpace(internalName))
        {
            throw new InvalidOperationException(
                $"Equipment item '{itemType.FullName}' has an empty InternalName!"
            );
        }

        if (_registrationsById.ContainsKey(internalName))
        {
            throw new InvalidOperationException(
                $"Equipment item ID '{internalName}' is already registered!"
            );
        }

        if (_registrationsByType.ContainsKey(itemType))
        {
            throw new InvalidOperationException($"Equipment item type '{itemType.FullName}' is already registered!");
        }

        var registration = new Registration(
            Definition: definition,
            Factory: factory
        );

        _registrationsById.Add(internalName, registration);
        _registrationsByType.Add(itemType, registration);
        
        return registration;
    }

    private static IItem Create(Registration registration)
    {
        var item = registration.Factory()
                   ?? throw new CannotCreateItemException($"Factory for '{registration.Definition.InternalName}' returned null!");

        if (item.GetType() != registration.Definition.GetType())
        {
            throw new CannotCreateItemException(
                $"Factory for '{registration.Definition.InternalName}' " +
                $"returned '{item.GetType().FullName}'."
            );
        }

        if (!string.Equals(item.InternalName, registration.Definition.InternalName, StringComparison.OrdinalIgnoreCase))
        {
            throw new CannotCreateItemException("Factory returned an item with a different InternalName!");
        }

        return item;
    }

    private static IItem Activate(Type itemType)
    {
        return Activator.CreateInstance(itemType, nonPublic: true) as IItem
            ?? throw new CannotCreateItemException($"Could not create equipment item '{itemType.FullName}'!");
    }

    private sealed record Registration(
        IItem Definition,
        Func<IItem> Factory
    );
    
    private void Unregister(Registration registration)
    {
        var internalName = registration.Definition.InternalName;
        var itemType = registration.Definition.GetType();

        if (!_registrationsById.TryGetValue(internalName, out var current) || !ReferenceEquals(current, registration))
        {
            return;
        }

        _registrationsById.Remove(internalName);
        _registrationsByType.Remove(itemType);
    }
    
    private sealed class RegistrationHandle(Action unregister) : IDisposable
    {
        private Action? _unregister = unregister;

        public void Dispose()
        {
            Interlocked.Exchange(ref _unregister, null)?.Invoke();
        }
    }
}