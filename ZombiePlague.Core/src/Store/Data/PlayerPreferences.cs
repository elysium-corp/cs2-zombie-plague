namespace ZombiePlague.Core.Store.Data;

internal sealed record PlayerPreferences
{
    public string ZClassId { get; init; } = "zombie_cleric";
    
    public string HClassId { get; init; } = "human_mercenary";

    public string KnifeId { get; init; } = "knife_ancient";
}