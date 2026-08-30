namespace Menu.Core.Database.Entities;

/// <summary>
/// Материализованный маршрут команды конкретного server-specific release.
/// </summary>
internal sealed class MenuCommandEntity
{
    public long Id { get; set; }
    public long ReleaseId { get; set; }
    public string ServerKey { get; set; } = string.Empty;
    public long RevisionId { get; set; }
    public string MenuKey { get; set; } = string.Empty;
    public string CommandType { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string NormalizedAlias { get; set; } = string.Empty;
    public string SuppressionMode { get; set; } = MenuDatabaseValues.SuppressionNone;
    public bool Enabled { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; }
    public MenuReleaseTargetEntity ReleaseTarget { get; set; } = null!;
    public MenuReleaseItemEntity ReleaseItem { get; set; } = null!;
}
