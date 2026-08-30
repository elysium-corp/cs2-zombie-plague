namespace Menu.Core.Database.Entities;

/// <summary>
/// Неизменяемый глобальный release набора меню.
/// </summary>
internal sealed class MenuReleaseEntity
{
    public long Id { get; set; }

    public long ReleaseNumber { get; set; }

    public int SchemaVersion { get; set; }

    public int MenuCoreApiVersion { get; set; }

    public long? RollbackOfReleaseId { get; set; }

    public string? CreatedBy { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset PublishedAt { get; set; }

    public MenuReleaseEntity? RollbackOfRelease { get; set; }

    public ICollection<MenuReleaseEntity> RollbackReleases { get; set; } = [];

    public ICollection<MenuReleaseItemEntity> Items { get; set; } = [];

    public ICollection<MenuReleaseTargetEntity> Targets { get; set; } = [];
}
