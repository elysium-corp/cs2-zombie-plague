namespace ZombiePlague.Core.Store.Data;

internal sealed record PlayerPreferences
{
    internal const string DefaultZombieClassId = "zombie_cleric";
    internal const string DefaultHumanClassId = "human_mercenary";
    internal const string DefaultKnifeId = "knife_spike";

    public string ZClassId { get; init; } = DefaultZombieClassId;

    public string HClassId { get; init; } = DefaultHumanClassId;

    public string KnifeId { get; init; } = DefaultKnifeId;
}
