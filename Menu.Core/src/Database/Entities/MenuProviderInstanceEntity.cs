namespace Menu.Core.Database.Entities;

/// <summary>
/// Состояние одной регистрации Provider на конкретном игровом сервере.
/// </summary>
internal sealed class MenuProviderInstanceEntity
{
    public long Id { get; set; }
    public long ProviderId { get; set; }
    public string ServerKey { get; set; } = string.Empty;
    public string PluginVersion { get; set; } = string.Empty;
    public int MenuApiVersion { get; set; }
    public string Status { get; set; } = MenuDatabaseValues.ProviderStatusOffline;
    public string CapabilitiesJson { get; set; } = "[]";
    public string MetadataJson { get; set; } = "{}";
    public Guid SessionId { get; set; }
    public long Generation { get; set; }
    public DateTimeOffset RegisteredAt { get; set; }
    public DateTimeOffset LastSeenAt { get; set; }
    public DateTimeOffset? OfflineAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public string? LastError { get; set; }
    public MenuProviderEntity Provider { get; set; } = null!;
    public ICollection<MenuProviderExportEntity> Exports { get; set; } = [];
}
