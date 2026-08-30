namespace Menu.Core.Database.Entities;

/// <summary>
/// Изменяемый working copy меню с явной optimistic locking версией.
/// </summary>
internal sealed class MenuDraftEntity
{
    public long Id { get; set; }
    public long DefinitionId { get; set; }
    public long? BaseRevisionId { get; set; }
    public int SchemaVersion { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public long LockVersion { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public MenuDefinitionEntity Definition { get; set; } = null!;
    public MenuRevisionEntity? BaseRevision { get; set; }
}
