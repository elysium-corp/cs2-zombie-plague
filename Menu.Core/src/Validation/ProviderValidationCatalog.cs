using System.Collections.Frozen;
using System.Text.Json;
using Menu.Api.Results;

namespace Menu.Core.Validation;

internal enum ProviderAvailability
{
    Offline = 0,
    Online = 1,
    Incompatible = 2,
    Error = 3
}

internal delegate MenuValidationResult ProviderArgumentValidator(JsonElement? arguments);

internal sealed class ProviderValidationEntry
{
    public ProviderValidationEntry(
        string providerKey,
        int menuApiVersion,
        ProviderAvailability availability,
        IEnumerable<string> menuKeys,
        IEnumerable<string> actionKeys,
        IReadOnlyDictionary<string, ProviderArgumentValidator>? argumentValidators = null,
        IReadOnlyDictionary<string, JsonElement>? argumentSchemas = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerKey);
        ArgumentNullException.ThrowIfNull(menuKeys);
        ArgumentNullException.ThrowIfNull(actionKeys);
        if (!MenuIdentifier.IsTechnicalKey(providerKey))
        {
            throw new ArgumentException("Provider key has an invalid format.", nameof(providerKey));
        }

        var menus = menuKeys.ToArray();
        var actions = actionKeys.ToArray();
        if (menus.Any(static key => !MenuIdentifier.IsTechnicalKey(key)))
        {
            throw new ArgumentException("Provider menu key has an invalid format.", nameof(menuKeys));
        }

        if (actions.Any(static key => !MenuIdentifier.IsTechnicalKey(key)))
        {
            throw new ArgumentException("Provider action key has an invalid format.", nameof(actionKeys));
        }

        if (menus.Distinct(StringComparer.Ordinal).Count() != menus.Length ||
            actions.Distinct(StringComparer.Ordinal).Count() != actions.Length)
        {
            throw new ArgumentException("Provider exports must have unique keys.");
        }

        var validators = argumentValidators ?? new Dictionary<string, ProviderArgumentValidator>();
        if (validators.Any(entry => !actions.Contains(entry.Key, StringComparer.Ordinal) || entry.Value is null))
        {
            throw new ArgumentException("Provider argument validators must reference registered actions.", nameof(argumentValidators));
        }

        if (!Enum.IsDefined(availability))
        {
            throw new ArgumentOutOfRangeException(nameof(availability));
        }

        if (availability == ProviderAvailability.Online && actions.Any(action => !validators.ContainsKey(action)))
        {
            throw new ArgumentException("Every online Provider action must have an argument validator.", nameof(argumentValidators));
        }

        var schemas = argumentSchemas ?? new Dictionary<string, JsonElement>();
        if (schemas.Any(entry => !actions.Contains(entry.Key, StringComparer.Ordinal)
                                 || entry.Value.ValueKind != JsonValueKind.Object))
        {
            throw new ArgumentException("Provider argument schemas must be objects for registered actions.", nameof(argumentSchemas));
        }

        ProviderKey = providerKey;
        MenuApiVersion = menuApiVersion;
        Availability = availability;
        MenuKeys = menus.ToFrozenSet(StringComparer.Ordinal);
        ActionKeys = actions.ToFrozenSet(StringComparer.Ordinal);
        ArgumentValidators = validators.ToFrozenDictionary(StringComparer.Ordinal);
        ArgumentSchemas = schemas.ToFrozenDictionary(
            static entry => entry.Key,
            static entry => entry.Value.Clone(),
            StringComparer.Ordinal);
    }

    public string ProviderKey { get; }

    public int MenuApiVersion { get; }

    public ProviderAvailability Availability { get; }

    public FrozenSet<string> MenuKeys { get; }

    public FrozenSet<string> ActionKeys { get; }

    public FrozenDictionary<string, ProviderArgumentValidator> ArgumentValidators { get; }

    public FrozenDictionary<string, JsonElement> ArgumentSchemas { get; }
}

internal sealed class ProviderValidationCatalog
{
    private readonly FrozenDictionary<string, ProviderValidationEntry> _providers;

    public ProviderValidationCatalog(IEnumerable<ProviderValidationEntry> providers)
    {
        ArgumentNullException.ThrowIfNull(providers);
        _providers = providers.ToFrozenDictionary(static provider => provider.ProviderKey, StringComparer.Ordinal);
    }

    public static ProviderValidationCatalog Empty { get; } = new([]);

    public IEnumerable<ProviderValidationEntry> Entries => _providers.Values;

    public bool TryGet(string providerKey, out ProviderValidationEntry provider)
    {
        return _providers.TryGetValue(providerKey, out provider!);
    }
}
