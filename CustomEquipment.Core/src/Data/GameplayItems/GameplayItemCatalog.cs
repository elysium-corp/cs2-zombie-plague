namespace CustomEquipment.Data.GameplayItems;

/// <summary>
/// Хранит атомарный runtime-снимок параметров встроенных гранат и оборудования.
/// </summary>
public sealed class GameplayItemCatalog
{
    private IReadOnlyDictionary<string, GameplayItemDefinition> _snapshot = CreateDefaults();

    internal GameplayItemDefinition Get(string implementationKey)
    {
        var snapshot = Volatile.Read(ref _snapshot);

        return snapshot.TryGetValue(implementationKey, out var definition)
            ? definition
            : throw new InvalidOperationException($"Gameplay item '{implementationKey}' is not configured.");
    }

    internal void Replace(IReadOnlyCollection<GameplayItemDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);

        var replacement = definitions.ToDictionary(
            definition => definition.ImplementationKey,
            StringComparer.Ordinal
        );

        var missing = GameplayItemDefaults.ImplementationKeys
            .Where(key => !replacement.ContainsKey(key))
            .ToArray();

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Gameplay item catalog is incomplete: {string.Join(", ", missing)}."
            );
        }

        Interlocked.Exchange(ref _snapshot, replacement);
    }

    private static IReadOnlyDictionary<string, GameplayItemDefinition> CreateDefaults()
    {
        return GameplayItemDefaults.All.ToDictionary(
            definition => definition.ImplementationKey,
            StringComparer.Ordinal
        );
    }
}
