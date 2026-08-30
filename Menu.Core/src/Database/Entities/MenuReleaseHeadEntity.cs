namespace Menu.Core.Database.Entities;

/// <summary>
/// Единственная изменяемая production-ссылка на release target сервера.
/// </summary>
internal sealed class MenuReleaseHeadEntity
{
    public string ServerKey { get; set; } = string.Empty;
    public long ReleaseId { get; set; }
    public long LockVersion { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public MenuReleaseTargetEntity Target { get; set; } = null!;
}
