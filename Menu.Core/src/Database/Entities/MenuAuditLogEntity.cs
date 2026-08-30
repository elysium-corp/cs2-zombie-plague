namespace Menu.Core.Database.Entities;

/// <summary>
/// Неизменяемая запись аудита управления меню.
/// </summary>
internal sealed class MenuAuditLogEntity
{
    public long Id { get; set; }
    public string? ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string? EntityKey { get; set; }
    public string? ServerKey { get; set; }
    public long? ReleaseId { get; set; }
    public long? RevisionId { get; set; }
    public string ChangesJson { get; set; } = "{}";
    public string MetadataJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public MenuReleaseEntity? Release { get; set; }
    public MenuRevisionEntity? Revision { get; set; }
}
