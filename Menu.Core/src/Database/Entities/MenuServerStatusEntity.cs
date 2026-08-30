namespace Menu.Core.Database.Entities;

/// <summary>
/// Диагностическое состояние одной runtime-инстанции Menu.Core.
/// </summary>
internal sealed class MenuServerStatusEntity
{
    public string ServerKey { get; set; } = string.Empty;

    public Guid RuntimeSessionId { get; set; }

    public long Generation { get; set; }

    public string MenuCoreVersion { get; set; } = string.Empty;

    public string SwiftlyVersion { get; set; } = string.Empty;

    public int MenuApiVersion { get; set; }

    public int SchemaVersion { get; set; }

    public string CapabilitiesJson { get; set; } = "{}";

    public long? ActiveReleaseId { get; set; }

    public string? ActiveChecksum { get; set; }

    public string? LoadedSource { get; set; }

    public DateTimeOffset? LastDbSyncAt { get; set; }

    public long? LastKnownGoodReleaseId { get; set; }

    public long? FallbackReleaseId { get; set; }

    public string ValidationStatus { get; set; } = "not_loaded";

    public string? LastError { get; set; }

    public DateTimeOffset HeartbeatAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }
}
