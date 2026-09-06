namespace ZombiePlague.Core.Store.Data;

internal sealed class PlayerPreferences
{
    internal const string DefaultZombieClassId = "zombie_cleric";
    internal const string DefaultHumanClassId = "human_mercenary";

    public string ZClassId { get; set; } = DefaultZombieClassId;

    public string HClassId { get; set; } = DefaultHumanClassId;

    public AbilityHudPreferences AbilityHud { get; set; } = AbilityHudPreferences.Default;

    // Сброс к стандартному виду до ответа БД тоже считается выбором игрока
    public bool AbilityHudScaleChangedInSession { get; set; }
    public bool AbilityHudPositionChangedInSession { get; set; }

    public void MergeLoaded(PlayerPreferences loaded)
    {
        if (ZClassId == DefaultZombieClassId) ZClassId = loaded.ZClassId;
        if (HClassId == DefaultHumanClassId) HClassId = loaded.HClassId;
        var loadedHud = loaded.AbilityHud.Normalize();
        AbilityHud = new(
            AbilityHudScaleChangedInSession ? AbilityHud.ScalePercent : loadedHud.ScalePercent,
            AbilityHudPositionChangedInSession ? AbilityHud.Position : loadedHud.Position);
    }

    public PlayerPreferences Snapshot() => new()
    {
        ZClassId = ZClassId,
        HClassId = HClassId,
        AbilityHud = AbilityHud.Normalize()
    };
}
