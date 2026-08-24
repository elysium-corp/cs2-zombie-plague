using Statistics.Core.Config;
using Statistics.Core.Data;
using Statistics.Core.Points;

namespace Statistics.Core.Tests;

public sealed class PointsFormulaTests
{
    private readonly PointsCalculator _calculator = new();

    [Fact]
    public void WebFormulaTimeoutDefaultsToFiveSeconds()
    {
        Assert.Equal(5, new PointsConfig().WebServiceTimeoutSeconds);
    }

    [Fact]
    public void BuiltInFormulaCalculatesExpectedRoundDelta()
    {
        var formula = PointsFormula.Parse(PointsConfig.BuiltInDefaultFormula);
        var context = new RoundPointsContext(
            ZombiesKilled: 3,
            InfectionsMade: 2,
            TimesInfected: 1,
            Deaths: 2,
            HumanWin: true,
            ZombieWin: false,
            BestKillStreak: 3,
            BestInfectionStreak: 2
        );

        var result = _calculator.CalculateDelta(formula, context);

        Assert.Equal(12, result);
    }

    [Fact]
    public void FormulaSupportsEveryPublishedVariable()
    {
        var formula = PointsFormula.Parse(
            "zombies_killed + infections_made + times_infected + deaths + " +
            "human_win + zombie_win + best_kill_streak + best_infection_streak"
        );
        var context = new RoundPointsContext(1, 2, 3, 4, true, false, 5, 6);

        var result = _calculator.CalculateDelta(formula, context);

        Assert.Equal(22, result);
    }

    [Theory]
    [InlineData("0.5", 1)]
    [InlineData("-0.5", -1)]
    [InlineData("1 + 2 * 3", 7)]
    [InlineData("(1 + 2) * 3", 9)]
    public void CalculatorUsesExpectedArithmeticAndRounding(string source, long expected)
    {
        var formula = PointsFormula.Parse(source);

        var result = _calculator.CalculateDelta(formula, default);

        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("unknown + 1")]
    [InlineData("Math.Abs(deaths)")]
    [InlineData("deaths ^ 2")]
    [InlineData("1 +")]
    public void ParserRejectsUnsupportedSyntax(string source)
    {
        Assert.Throws<PointsFormulaException>(() => PointsFormula.Parse(source));
    }

    [Fact]
    public void CalculatorRejectsDivisionByZero()
    {
        var formula = PointsFormula.Parse("zombies_killed / deaths");

        Assert.Throws<PointsFormulaException>(
            () => _calculator.CalculateDelta(formula, default)
        );
    }
}
