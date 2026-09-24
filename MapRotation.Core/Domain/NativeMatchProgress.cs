namespace MapRotation.Core.Domain;

/// <summary>Наблюдение за лимитами CS2 без изменения игровых правил и настроек матча.</summary>
internal sealed record NativeMatchProgress(bool Warmup, bool Started, bool Ended,
    double? SecondsRemaining, int? RoundsRemaining)
{
    public static double? RemainingSeconds(double minutes, double currentTime, double gameStartTime) =>
        double.IsFinite(minutes) && minutes > 0 && minutes <= 604800 / 60d
        && double.IsFinite(currentTime) && double.IsFinite(gameStartTime) && gameStartTime >= 0 && currentTime >= gameStartTime
            ? Math.Max(0, minutes * 60 - (currentTime - gameStartTime)) : null;

    public static int? RemainingRounds(int maxRounds, int played, bool canClinch, int winLimit, int highestScore)
    {
        int? remaining = maxRounds > 0 ? Math.Max(0, maxRounds - Math.Max(0, played)) : null;
        if (maxRounds > 0 && canClinch)
            remaining = Math.Min(remaining!.Value, Math.Max(0, maxRounds / 2 + 1 - Math.Max(0, highestScore)));
        if (winLimit > 0)
        {
            var wins = Math.Max(0, winLimit - Math.Max(0, highestScore));
            remaining = remaining.HasValue ? Math.Min(remaining.Value, wins) : wins;
        }
        return remaining;
    }
}
