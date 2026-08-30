namespace Menu.Core.Database.Models;

/// <summary>
/// Полный снимок одной активной Provider-сессии, пригодный для reconcile после сбоя БД.
/// </summary>
internal sealed record MenuProviderPersistenceSnapshot(
    string ProviderKey,
    string DisplayName,
    string PluginVersion,
    int MenuApiVersion,
    string CapabilitiesJson,
    string MetadataJson,
    string Status,
    string? LastError,
    Guid SessionId,
    long Generation,
    IReadOnlyCollection<MenuProviderExportPersistence> Exports);

/// <summary>Сериализуемая часть Provider export без runtime-делегатов.</summary>
internal sealed record MenuProviderExportPersistence(
    string ExportType,
    string ExportKey,
    string DisplayName,
    string? SchemaJson,
    string MetadataJson);
