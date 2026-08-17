namespace ZombiePlague.Core.Store.Data;

internal sealed class PlayerPreferences
{
    internal const string DefaultZombieClassId = "zombie_cleric";
    internal const string DefaultHumanClassId = "human_mercenary";
    internal const string DefaultKnifeId = "knife_spike";

    public string ZClassId { get; set; } = DefaultZombieClassId;

    public string HClassId { get; set; } = DefaultHumanClassId;

    public string KnifeId { get; set; } = DefaultKnifeId;
}