namespace Menu.Core.Database.Entities;

/// <summary>
/// Неизменяемая опубликованная revision меню.
/// </summary>
internal sealed class MenuRevisionEntity
{
    public long Id { get; set; }
    public long DefinitionId { get; set; }
    public int RevisionNumber { get; set; }
    public int SchemaVersion { get; set; }
    public string PayloadJson { get; set; } = "{}";
    public string Checksum { get; set; } = string.Empty;
    public long? BasedOnRevisionId { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset PublishedAt { get; set; }
    public MenuDefinitionEntity Definition { get; set; } = null!;
    public MenuRevisionEntity? BasedOnRevision { get; set; }
    public ICollection<MenuRevisionEntity> DerivedRevisions { get; set; } = [];
    public ICollection<MenuReleaseItemEntity> ReleaseItems { get; set; } = [];
}
