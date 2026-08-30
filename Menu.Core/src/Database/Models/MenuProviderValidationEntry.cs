namespace Menu.Core.Database.Models;

/// <summary>
/// Cold-start каталог Provider с последним известным набором exports.
/// Status определяет доступность Provider, IsDeclared — наличие export в последнем reconcile.
/// </summary>
internal sealed record MenuProviderValidationEntry(
    string ProviderKey,
    string DisplayName,
    string ServerKey,
    string PluginVersion,
    int MenuApiVersion,
    string Status,
    string CapabilitiesJson,
    string MetadataJson,
    Guid SessionId,
    long Generation,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset? OfflineAt,
    DateTimeOffset UpdatedAt,
    string? LastError,
    IReadOnlyList<MenuProviderExportValidationEntry> ExportHistory)
{
    /// <summary>
    /// Единственный набор, допустимый для dependency validation. Undeclared rows
    /// остаются только в ExportHistory для CMS diagnostics.
    /// </summary>
    internal IReadOnlyList<MenuProviderExportValidationEntry> DeclaredExports { get; } =
        ExportHistory.Where(export => export.IsDeclared).ToArray();
}

/// <summary>Текущий или исторический export Provider для publish validation.</summary>
internal sealed record MenuProviderExportValidationEntry(
    string ExportType,
    string ExportKey,
    string DisplayName,
    string? SchemaJson,
    string MetadataJson,
    bool IsDeclared,
    long DeclaredGeneration,
    DateTimeOffset FirstSeenAt,
    DateTimeOffset LastSeenAt,
    DateTimeOffset UpdatedAt);
