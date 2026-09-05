using SupplyBox.Data.Configs;

namespace SupplyBox.Configuration;

internal static class SupplyBoxRules
{
    public static bool RoundAllows(SupplyBoxConfig settings, int number, bool survivor, bool nemesis) =>
        number >= settings.StartFromRound && (number - settings.StartFromRound) % settings.EveryNthRound == 0
        && (settings.AllowSurvivorRound || !survivor) && (settings.AllowNemesisRound || !nemesis);

    public static bool PopulationAllows(SupplyBoxConfig settings, int players, int humans, int zombies) =>
        players >= settings.MinPlayers && humans >= settings.MinAliveHumans && zombies >= settings.MinAliveZombies;

    public static bool LimitReached(SupplyBoxConfig settings, SupplyBoxMap map, int active, int roundDrops, int mapDrops) =>
        active >= (map.MaxCountTogether ?? settings.MaxCountTogether)
        || (settings.MaxDropsPerRound > 0 && roundDrops >= settings.MaxDropsPerRound)
        || (settings.MaxDropsPerMap > 0 && mapDrops >= settings.MaxDropsPerMap);
}
