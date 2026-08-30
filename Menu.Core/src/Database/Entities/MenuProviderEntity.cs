namespace Menu.Core.Database.Entities;

/// <summary>
/// Стабильная запись Provider, общая для всех игровых серверов.
/// </summary>
internal sealed class MenuProviderEntity
{
    public long Id { get; set; }

    public string ProviderKey { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string MetadataJson { get; set; } = "{}";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset UpdatedAt { get; set; }

    public ICollection<MenuProviderInstanceEntity> Instances { get; set; } = [];
}
