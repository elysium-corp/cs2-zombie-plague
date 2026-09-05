using System.Text.RegularExpressions;

namespace SupplyBox.Configuration;

internal static class SupplyBoxSoundEvents
{
    internal const int MaximumCount = 16;

    public static void Validate(IReadOnlyList<string>? events)
    {
        if (events is null || events.Count > MaximumCount)
            throw new InvalidDataException("Звуки сброса: ожидается список из 0–16 soundevent");
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in events)
        {
            if (name is null || !Regex.IsMatch(name, @"\A[A-Za-z0-9_.-]{1,128}\z") || !seen.Add(name))
                throw new InvalidDataException("Звуки сброса: нужны уникальные имена soundevent до 128 символов без пробелов");
        }
    }

    public static string? Choose(IReadOnlyList<string> events, Func<int, int> randomIndex) =>
        events.Count == 0 ? null : events[randomIndex(events.Count)];
}
