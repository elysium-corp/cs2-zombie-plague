using System.Text.Json;
using Menu.Api.Contracts;
using Menu.Core.Configuration;
using Menu.Core.Database;
using Menu.Core.Database.Models;
using Menu.Core.Providers;
using Menu.Core.Swiftly;
using Menu.Core.Validation;
using Microsoft.Extensions.Options;

namespace Menu.Core.Application;

internal sealed class MenuValidationContextFactory(
    ProviderRegistry providers,
    MenuCapabilityProvider capabilities,
    IOptions<MenuCoreConfig> options)
{
    private readonly MenuCoreConfig _configuration = options.Value;

    internal MenuReleaseValidationContext Create(
        MenuReleaseDefinition? release = null,
        IEnumerable<MenuProviderValidationEntry>? persistedProviders = null)
    {
        var mergedProviders = new Dictionary<string, ProviderValidationEntry>(StringComparer.Ordinal);
        AddArtifactContracts(mergedProviders, release);

        if (persistedProviders is not null)
        {
            foreach (var provider in persistedProviders)
            {
                if (!TryReadPersistedSchemas(provider.DeclaredExports, out var schemas))
                {
                    continue;
                }

                var availability = provider.Status switch
                {
                    MenuDatabaseValues.ProviderStatusIncompatible or
                        MenuDatabaseValues.ProviderStatusApiOutdated => ProviderAvailability.Incompatible,
                    MenuDatabaseValues.ProviderStatusError => ProviderAvailability.Error,
                    _ => ProviderAvailability.Offline
                };
                TryAdd(
                    mergedProviders,
                    provider.ProviderKey,
                    provider.MenuApiVersion,
                    availability,
                    provider.DeclaredExports
                        .Where(static export => export.ExportType == MenuDatabaseValues.ExportTypeMenu)
                        .Select(static export => export.ExportKey),
                    provider.DeclaredExports
                        .Where(static export => export.ExportType == MenuDatabaseValues.ExportTypeAction)
                        .Select(static export => export.ExportKey),
                    schemas);
            }
        }

        // Живой registry имеет приоритет: только он располагает исполняемыми
        // argument validators и достоверно отражает доступность в этом процессе.
        foreach (var provider in providers.BuildValidationCatalog().Entries)
        {
            mergedProviders[provider.ProviderKey] = provider;
        }

        return new MenuReleaseValidationContext(
            _configuration.ServerKey,
            _configuration.ServerGroups,
            capabilities.Current,
            new ProviderValidationCatalog(mergedProviders.Values),
            _configuration.ReservedCommands,
            _configuration.MaxNavigationDepth);
    }

    private static void AddArtifactContracts(
        IDictionary<string, ProviderValidationEntry> destination,
        MenuReleaseDefinition? release)
    {
        if (release?.Metadata is null
            || !release.Metadata.TryGetValue("providerContracts", out var contracts)
            || contracts.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var contract in contracts.EnumerateArray())
        {
            if (contract.ValueKind != JsonValueKind.Object
                || !TryReadString(contract, "providerKey", out var providerKey)
                || !contract.TryGetProperty("menuApiVersion", out var apiVersionElement)
                || !apiVersionElement.TryGetInt32(out var apiVersion)
                || !TryReadKeys(contract, "menuKeys", out var menuKeys)
                || !TryReadKeys(contract, "actionKeys", out var actionKeys)
                || !TryReadSchemas(contract, "actionSchemas", out var schemas))
            {
                continue;
            }

            TryAdd(destination, providerKey, apiVersion, ProviderAvailability.Offline, menuKeys, actionKeys, schemas);
        }
    }

    private static void TryAdd(
        IDictionary<string, ProviderValidationEntry> destination,
        string providerKey,
        int menuApiVersion,
        ProviderAvailability availability,
        IEnumerable<string> menuKeys,
        IEnumerable<string> actionKeys,
        IReadOnlyDictionary<string, JsonElement>? argumentSchemas = null)
    {
        try
        {
            destination[providerKey] = new ProviderValidationEntry(
                providerKey,
                menuApiVersion,
                availability,
                menuKeys,
                actionKeys,
                argumentSchemas: argumentSchemas);
        }
        catch (ArgumentException)
        {
            // Повреждённым сохранённым и встроенным контрактам доверять нельзя.
            // При ссылке на такой контракт release validator вернёт provider.missing.
        }
    }

    private static bool TryReadString(JsonElement parent, string name, out string value)
    {
        value = string.Empty;
        return parent.TryGetProperty(name, out var element)
               && element.ValueKind == JsonValueKind.String
               && (value = element.GetString() ?? string.Empty).Length > 0;
    }

    private static bool TryReadKeys(JsonElement parent, string name, out string[] keys)
    {
        keys = [];
        if (!parent.TryGetProperty(name, out var element) || element.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var values = new List<string>();
        foreach (var item in element.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String || string.IsNullOrEmpty(item.GetString()))
            {
                return false;
            }

            values.Add(item.GetString()!);
        }

        keys = values.ToArray();
        return true;
    }

    private static bool TryReadSchemas(
        JsonElement parent,
        string name,
        out IReadOnlyDictionary<string, JsonElement> schemas)
    {
        schemas = new Dictionary<string, JsonElement>();
        if (!parent.TryGetProperty(name, out var element))
        {
            return true;
        }

        if (element.ValueKind != JsonValueKind.Object)
        {
            return false;
        }

        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var property in element.EnumerateObject())
        {
            if (!MenuIdentifier.IsTechnicalKey(property.Name)
                || property.Value.ValueKind != JsonValueKind.Object
                || !values.TryAdd(property.Name, property.Value.Clone()))
            {
                return false;
            }
        }

        schemas = values;
        return true;
    }

    private static bool TryReadPersistedSchemas(
        IEnumerable<MenuProviderExportValidationEntry> exports,
        out IReadOnlyDictionary<string, JsonElement> schemas)
    {
        var values = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        foreach (var export in exports.Where(static export =>
                     export.ExportType == MenuDatabaseValues.ExportTypeAction
                     && export.SchemaJson is not null))
        {
            try
            {
                using var document = JsonDocument.Parse(export.SchemaJson!);
                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    schemas = new Dictionary<string, JsonElement>();
                    return false;
                }

                values[export.ExportKey] = document.RootElement.Clone();
            }
            catch (JsonException)
            {
                schemas = new Dictionary<string, JsonElement>();
                return false;
            }
        }

        schemas = values;
        return true;
    }
}
