using System.Globalization;

namespace Shop.Core.Hud;

/// <summary>Короткая подпись фиксированного таймера, включая длительные ограничения покупки.</summary>
internal static class ShopHudCountdown
{
    internal static string Format(int seconds, string format, Func<string, string>? unit = null)
    {
        seconds = Math.Max(0, seconds);
        string Symbol(string key, string fallback)
        {
            var value = unit?.Invoke("Shop.Hud.Timer." + key);
            return string.IsNullOrEmpty(value) || value.StartsWith("Shop.Hud.Timer.", StringComparison.Ordinal) ? fallback : value;
        }
        if (format == "seconds") return seconds > 99999 ? "99999+" : seconds.ToString(CultureInfo.InvariantCulture);
        if (format == "clock" && seconds < 6000) return $"{seconds / 60:00}:{seconds % 60:00}";
        if (seconds < 60) return seconds + Symbol("Seconds", "s");
        if (seconds < 3600) return ((seconds + 59L) / 60) + Symbol("Minutes", "m");
        if (seconds < 86400) return ((seconds + 3599L) / 3600) + Symbol("Hours", "h");
        var days = (seconds + 86399L) / 86400;
        return (days > 999 ? "999" : days.ToString(CultureInfo.InvariantCulture)) + Symbol("Days", "d") + (days > 999 ? "+" : "");
    }

    internal static int Progress(int remaining, int duration) => duration <= 0 ? 0
        : (int)Math.Clamp(Math.Ceiling(remaining * 100d / duration), 0, 100);
}
