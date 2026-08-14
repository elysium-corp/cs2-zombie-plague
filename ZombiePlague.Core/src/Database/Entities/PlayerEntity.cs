using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Database.Entities;

internal sealed class PlayerEntity
{
    public int Id { get; set; }

    public long SteamId { get; set; }

    public string ZombieClassId { get; set; } = PlayerPreferences.DefaultZombieClassId;

    public string HumanClassId { get; set; } = PlayerPreferences.DefaultHumanClassId;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
