namespace Menu.Core.Database.Entities;

/// <summary>
/// Стабильная идентичность меню, не зависящая от его revisions.
/// </summary>
internal sealed class MenuDefinitionEntity
{
    public long Id { get; set; }
    public string MenuKey { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? OwnerProviderKey { get; set; }
    public string Status { get; set; } = MenuDatabaseValues.DefinitionStatusDraft;
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public MenuDraftEntity? Draft { get; set; }
    public ICollection<MenuRevisionEntity> Revisions { get; set; } = [];
    public ICollection<MenuReleaseItemEntity> ReleaseItems { get; set; } = [];
}
