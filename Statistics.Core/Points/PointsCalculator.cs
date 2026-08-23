using Statistics.Core.Data;

namespace Statistics.Core.Points;

internal sealed class PointsCalculator
{
    public long CalculateDelta(PointsFormula formula, RoundPointsContext context)
    {
        ArgumentNullException.ThrowIfNull(formula);

        var result = decimal.Round(
            formula.Evaluate(context),
            decimals: 0,
            mode: MidpointRounding.AwayFromZero
        );

        if (result is > long.MaxValue or < long.MinValue)
        {
            throw new PointsFormulaException(
                "Points formula result is outside the supported Int64 range."
            );
        }

        return decimal.ToInt64(result);
    }
}
