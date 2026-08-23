namespace Statistics.Core.Config;

internal sealed class StatisticsConfig
{
    public PointsConfig Points { get; set; } = new();
}

internal sealed class PointsConfig
{
    public const string BuiltInDefaultFormula =
        "zombies_killed * 2 + infections_made * 3 - deaths * 2 " +
        "- times_infected + human_win * 5 + zombie_win * 5";

    public string DefaultFormula { get; set; } = BuiltInDefaultFormula;

    public string WebServiceFormulaUrl { get; set; } = string.Empty;

    public int WebServiceTimeoutSeconds { get; set; } = 3;
}
