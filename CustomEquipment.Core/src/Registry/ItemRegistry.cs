using System.Diagnostics.CodeAnalysis;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Exceptions;
using CustomEquipment.Data.DatabaseWeapons;
using CustomEquipment.Fetcher;

namespace CustomEquipment.Registry;

internal sealed class ItemRegistry(IEquipmentFetcher equipmentFetcher) : IItemRegistry
{
    private readonly object _sync = new();
    private readonly Dictionary<string, Registration> _registrationsById = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, Registration> _registrationsByType = [];

    public void Initialize()
    {
        var builtIns = equipmentFetcher.Fetch()
            .Select(definition => CreateRegistration(
                definition,
                factory: () => Activate(definition.GetType()),
                source: RegistrationSource.BuiltIn,
                registerByType: true
            ))
            .ToArray();

        lock (_sync)
        {
            var preserved = _registrationsById.Values
                .Where(registration => registration.Source == RegistrationSource.External)
                .ToArray();

            ReplaceAll([.. preserved, .. builtIns]);
        }
    }

    public void ReplaceDatabaseWeapons(IReadOnlyCollection<DatabaseWeaponItem> weapons)
    {
        ArgumentNullException.ThrowIfNull(weapons);

        var databaseRegistrations = weapons
            .Select(definition => CreateRegistration(
                definition,
                factory: definition.CreateInstance,
                source: RegistrationSource.Database,
                registerByType: false
            ))
            .ToArray();

        lock (_sync)
        {
            var preserved = _registrationsById.Values
                .Where(registration => registration.Source != RegistrationSource.Database)
                .ToArray();

            ReplaceAll([.. preserved, .. databaseRegistrations]);
        }
    }

    public IReadOnlyCollection<IItem> GetDefinitions()
    {
        lock (_sync)
        {
            return _registrationsById.Values
                .Select(registration => registration.Definition)
                .ToArray();
        }
    }

    public bool TryGetDefinition(string internalName, [NotNullWhen(true)] out IItem? definition)
    {
        definition = null;

        if (string.IsNullOrWhiteSpace(internalName))
        {
            return false;
        }

        lock (_sync)
        {
            if (!_registrationsById.TryGetValue(internalName, out var registration))
            {
                return false;
            }

            definition = registration.Definition;
            return true;
        }
    }

    public TItem Create<TItem>() where TItem : class, IItem
    {
        Registration registration;

        lock (_sync)
        {
            var itemType = typeof(TItem);

            if (!_registrationsByType.TryGetValue(itemType, out registration!))
            {
                throw new NotRegisteredItemException($"Equipment item '{itemType.FullName}' is not registered!");
            }
        }

        return Create(registration) as TItem
            ?? throw new CannotCreateItemException($"Registered factory returned an invalid '{typeof(TItem).FullName}'!");
    }

    public IItem Create(string internalName)
    {
        if (string.IsNullOrWhiteSpace(internalName))
        {
            throw new NotRegisteredItemException("Equipment item ID cannot be empty!");
        }

        Registration registration;

        lock (_sync)
        {
            if (!_registrationsById.TryGetValue(internalName, out registration!))
            {
                throw new NotRegisteredItemException($"Equipment item '{internalName}' is not registered!");
            }
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
                $"Factory registered as '{typeof(TItem).FullName}' returned '{definition.GetType().FullName}'!"
            );
        }

        var registration = CreateRegistration(
            definition,
            factory: () => factory() ?? throw new CannotCreateItemException(
                $"Factory for '{typeof(TItem).FullName}' returned null!"
            ),
            source: RegistrationSource.External,
            registerByType: true
        );

        lock (_sync)
        {
            EnsureCanAdd(registration, _registrationsById, _registrationsByType);
            Add(registration, _registrationsById, _registrationsByType);
        }

        return new RegistrationHandle(() => Unregister(registration));
    }

    private void ReplaceAll(IReadOnlyCollection<Registration> registrations)
    {
        var byId = new Dictionary<string, Registration>(StringComparer.OrdinalIgnoreCase);
        var byType = new Dictionary<Type, Registration>();

        foreach (var registration in registrations)
        {
            EnsureCanAdd(registration, byId, byType);
            Add(registration, byId, byType);
        }

        _registrationsById.Clear();
        _registrationsByType.Clear();

        foreach (var pair in byId)
        {
            _registrationsById.Add(pair.Key, pair.Value);
        }

        foreach (var pair in byType)
        {
            _registrationsByType.Add(pair.Key, pair.Value);
        }
    }

    private static Registration CreateRegistration(
        IItem definition,
        Func<IItem> factory,
        RegistrationSource source,
        bool registerByType
    )
    {
        var internalName = definition.InternalName;
        var itemType = definition.GetType();

        if (string.IsNullOrWhiteSpace(internalName))
        {
            throw new InvalidOperationException($"Equipment item '{itemType.FullName}' has an empty InternalName!");
        }

        return new Registration(definition, factory, source, registerByType);
    }

    private static void EnsureCanAdd(
        Registration registration,
        IReadOnlyDictionary<string, Registration> byId,
        IReadOnlyDictionary<Type, Registration> byType
    )
    {
        var internalName = registration.Definition.InternalName;
        var itemType = registration.Definition.GetType();

        if (byId.ContainsKey(internalName))
        {
            throw new InvalidOperationException($"Equipment item ID '{internalName}' is already registered!");
        }

        if (registration.RegisterByType && byType.ContainsKey(itemType))
        {
            throw new InvalidOperationException($"Equipment item type '{itemType.FullName}' is already registered!");
        }
    }

    private static void Add(
        Registration registration,
        IDictionary<string, Registration> byId,
        IDictionary<Type, Registration> byType
    )
    {
        byId.Add(registration.Definition.InternalName, registration);

        if (registration.RegisterByType)
        {
            byType.Add(registration.Definition.GetType(), registration);
        }
    }

    private static IItem Create(Registration registration)
    {
        var item = registration.Factory()
                   ?? throw new CannotCreateItemException(
                       $"Factory for '{registration.Definition.InternalName}' returned null!"
                   );

        if (item.GetType() != registration.Definition.GetType())
        {
            throw new CannotCreateItemException(
                $"Factory for '{registration.Definition.InternalName}' returned '{item.GetType().FullName}'."
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

    private void Unregister(Registration registration)
    {
        lock (_sync)
        {
            var internalName = registration.Definition.InternalName;

            if (!_registrationsById.TryGetValue(internalName, out var current) ||
                !ReferenceEquals(current, registration))
            {
                return;
            }

            _registrationsById.Remove(internalName);

            if (registration.RegisterByType)
            {
                var itemType = registration.Definition.GetType();

                if (_registrationsByType.TryGetValue(itemType, out current) && ReferenceEquals(current, registration))
                {
                    _registrationsByType.Remove(itemType);
                }
            }
        }
    }

    private sealed record Registration(
        IItem Definition,
        Func<IItem> Factory,
        RegistrationSource Source,
        bool RegisterByType
    );

    private enum RegistrationSource
    {
        BuiltIn,
        Database,
        External
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
