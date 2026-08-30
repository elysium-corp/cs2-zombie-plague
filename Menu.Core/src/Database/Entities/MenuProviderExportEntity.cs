namespace Menu.Core.Database.Entities;

/// <summary>
/// Экспортированное Provider меню или действие.
/// </summary>
internal sealed class MenuProviderExportEntity
{
    public long Id { get; set; }

    public long ProviderInstanceId { get; set; }

    public string ExportType { get; set; } = string.Empty;

    public string ExportKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? SchemaJson { get; set; }

    public string MetadataJson { get; set; } = "{}";

    public bool IsDeclared { get; set; } = true;

    public long DeclaredGeneration { get; set; }

    public DateTimeOffset FirstSeenAt { get; set; }

    public DateTimeOffset LastSeenAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public MenuProviderInstanceEntity ProviderInstance { get; set; } = null!;
}
