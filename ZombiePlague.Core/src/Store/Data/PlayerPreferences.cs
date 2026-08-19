namespace ZombiePlague.Core.Store.Data;

internal sealed class PlayerPreferences
{
    internal const string DefaultZombieClassId = "zombie_cleric";
    internal const string DefaultHumanClassId = "human_mercenary";

    public string ZClassId { get; set; } = DefaultZombieClassId;

    public string HClassId { get; set; } = DefaultHumanClassId;
}