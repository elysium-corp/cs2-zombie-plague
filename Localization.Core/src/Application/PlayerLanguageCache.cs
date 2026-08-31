namespace Localization.Core.Application;

internal sealed class PlayerLanguageCache
{
    private readonly ConcurrentDictionary<ulong, Entry> _entries = new();

    public long BeginLoad(ulong steamId)
    {
        var entry = _entries.GetOrAdd(steamId, static _ => new Entry());
        lock (entry.Sync)
        {
            return ++entry.Generation;
        }
    }

    public void CompleteLoad(ulong steamId, long generation, string? languageCode)
    {
        if (!_entries.TryGetValue(steamId, out var entry))
        {
            return;
        }

        lock (entry.Sync)
        {
            if (entry.Generation != generation)
            {
                return;
            }

            entry.LanguageCode = NormalizeNullable(languageCode);
            entry.Loaded = true;
        }
    }

    public void SetManual(ulong steamId, string languageCode)
    {
        var entry = _entries.GetOrAdd(steamId, static _ => new Entry());
        lock (entry.Sync)
        {
            entry.Generation++;
            entry.LanguageCode = LocaleNormalizer.Normalize(languageCode);
            entry.Loaded = true;
        }
    }

    public bool TryGetManual(ulong steamId, out string? languageCode)
    {
        languageCode = null;
        if (!_entries.TryGetValue(steamId, out var entry))
        {
            return false;
        }

        lock (entry.Sync)
        {
            if (!entry.Loaded)
            {
                return false;
            }

            languageCode = entry.LanguageCode;
            return true;
        }
    }

    public void Remove(ulong steamId)
    {
        _entries.TryRemove(steamId, out _);
    }

    private static string? NormalizeNullable(string? languageCode)
    {
        var normalized = LocaleNormalizer.Normalize(languageCode);
        return normalized.Length == 0 ? null : normalized;
    }

    private sealed class Entry
    {
        public object Sync { get; } = new();
        public long Generation { get; set; }
        public string? LanguageCode { get; set; }
        public bool Loaded { get; set; }
    }
}
