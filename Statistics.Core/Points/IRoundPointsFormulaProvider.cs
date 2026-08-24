namespace Statistics.Core.Points;

internal interface IRoundPointsFormulaProvider
{
    void Start();

    PointsFormula CaptureFormula();

    void RefreshAndWait();

    void StopAndWait();
}
