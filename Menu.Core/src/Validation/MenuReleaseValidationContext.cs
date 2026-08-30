using System.Collections.Frozen;
using Menu.Api.Contracts;
using Menu.Api.Enums;

namespace Menu.Core.Validation;

internal sealed class MenuReleaseValidationContext
{
    public MenuReleaseValidationContext(
        string serverKey,
        IEnumerable<string> serverGroups,
        MenuCapabilityManifest capabilities,
        ProviderValidationCatalog providers,
        IEnumerable<string> reservedConsoleCommands,
        int maximumNavigationDepth)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serverKey);
        ArgumentNullException.ThrowIfNull(serverGroups);
        ArgumentNullException.ThrowIfNull(capabilities);
        ArgumentNullException.ThrowIfNull(providers);
        ArgumentNullException.ThrowIfNull(reservedConsoleCommands);
        ArgumentOutOfRangeException.ThrowIfLessThan(maximumNavigationDepth, 1);
        if (!MenuIdentifier.IsTechnicalKey(serverKey))
        {
            throw new ArgumentException("Server key has an invalid format.", nameof(serverKey));
        }

        var groups = serverGroups.ToArray();
        if (groups.Any(static group => !MenuIdentifier.IsTechnicalKey(group)))
        {
            throw new ArgumentException("Server group key has an invalid format.", nameof(serverGroups));
        }

        ServerKey = serverKey;
        ServerGroups = groups.ToFrozenSet(StringComparer.Ordinal);
        Capabilities = capabilities with
        {
            Features = (capabilities.Features ?? new Dictionary<string, bool>())
                .ToFrozenDictionary(StringComparer.Ordinal)
        };
        Providers = providers;
        ReservedCommandLookupKeys = reservedConsoleCommands
            .Select(static alias => MenuIdentifier.CommandLookupKey(MenuCommandKind.Console, alias))
            .ToFrozenSet(StringComparer.Ordinal);
        MaximumNavigationDepth = maximumNavigationDepth;
    }

    public string ServerKey { get; }

    public FrozenSet<string> ServerGroups { get; }

    public MenuCapabilityManifest Capabilities { get; }

    public ProviderValidationCatalog Providers { get; }

    public FrozenSet<string> ReservedCommandLookupKeys { get; }

    public int MaximumNavigationDepth { get; }
}
