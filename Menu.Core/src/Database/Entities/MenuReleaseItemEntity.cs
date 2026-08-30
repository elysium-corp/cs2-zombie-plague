namespace Menu.Core.Database.Entities;

/// <summary>
/// Точная revision меню, входящая в глобальный release.
/// </summary>
internal sealed class MenuReleaseItemEntity
{
    public long ReleaseId { get; set; }
    public long DefinitionId { get; set; }
    public long RevisionId { get; set; }
    public MenuReleaseEntity Release { get; set; } = null!;
    public MenuDefinitionEntity Definition { get; set; } = null!;
    public MenuRevisionEntity Revision { get; set; } = null!;
    public ICollection<MenuCommandEntity> Commands { get; set; } = [];
}
