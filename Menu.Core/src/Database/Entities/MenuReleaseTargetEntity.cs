namespace Menu.Core.Database.Entities;

/// <summary>
/// Готовый неизменяемый runtime artifact release для конкретного сервера.
/// </summary>
internal sealed class MenuReleaseTargetEntity
{
    public long ReleaseId { get; set; }
    public string ServerKey { get; set; } = string.Empty;
    public string? ServerGroupKey { get; set; }
    public string ArtifactJson { get; set; } = "{}";
    public string Checksum { get; set; } = string.Empty;
    public string CapabilityManifestJson { get; set; } = "{}";
    public DateTimeOffset CreatedAt { get; set; }
    public MenuReleaseEntity Release { get; set; } = null!;
    public MenuReleaseHeadEntity? Head { get; set; }
    public ICollection<MenuCommandEntity> Commands { get; set; } = [];
}
