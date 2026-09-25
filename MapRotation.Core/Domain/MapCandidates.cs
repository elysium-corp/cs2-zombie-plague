using System.Collections.Immutable;

namespace MapRotation.Core.Domain;

internal sealed class MapCandidates(IRotationRandom random)
{
    public RotationMap? Weighted(IEnumerable<RotationMap> source)
    {
        var maps = source.ToArray();
        if (maps.Length == 0) return null;
        var pick = random.NextDouble() * maps.Sum(map => map.Weight);
        foreach (var map in maps)
        {
            pick -= map.Weight;
            if (pick < 0) return map;
        }
        return maps[^1];
    }

    public T Tie<T>(IReadOnlyList<T> items) => items[Math.Min(items.Count - 1, (int)(random.NextDouble() * items.Count))];

    public ImmutableArray<RotationMap> Eligible(RotationConfiguration config, string current, string workshop,
        IReadOnlyList<long> history, IReadOnlySet<long> valid, Func<RotationMap, bool> purpose, int desired)
    {
        var safe = config.Maps.Where(map => map.Enabled && valid.Contains(map.Id) && purpose(map)
            && (config.Settings.AllowSameMap || !map.IsCurrent(current, workshop))).ToArray();
        bool Recent(RotationMap map, int count) => history.Take(count).Contains(map.Id);
        var strict = safe.Where(map => !Recent(map, config.Settings.RecentMapsExcluded)
            && !Recent(map, map.CooldownMaps ?? 0)).ToList();
        // Одинаковый снимок всегда даёт одинаковый пул для показа и проверки номинации.
        void Fill(IEnumerable<RotationMap> pool)
        {
            strict.AddRange(pool.Where(map => strict.All(selected => selected.Id != map.Id)));
        }
        if (strict.Count < desired) Fill(safe.Where(map => !Recent(map, config.Settings.RecentMapsExcluded)));
        if (strict.Count < desired) Fill(safe);
        return strict.ToImmutableArray();
    }

    public ImmutableArray<RotationMap> VoteOptions(ImmutableArray<RotationMap> eligible,
        IReadOnlyDictionary<ulong, long> nominations, RotationSettings settings)
    {
        var limit = Math.Clamp(settings.VoteOptionsCount, 1, 6);
        var nominationLimit = Math.Clamp(settings.NominationSlots, 0, limit);
        List<RotationMap> selected = [];
        foreach (var group in nominations.Values.GroupBy(id => id).GroupBy(group => group.Count()).OrderByDescending(g => g.Key))
        {
            var tied = group.Select(entry => eligible.FirstOrDefault(map => map.Id == entry.Key)).OfType<RotationMap>().ToList();
            while (tied.Count > 0 && selected.Count < nominationLimit)
            {
                var map = Tie(tied); tied.Remove(map); selected.Add(map);
            }
        }
        var remaining = eligible.Where(map => selected.All(item => item.Id != map.Id)).ToList();
        while (selected.Count < limit && Weighted(remaining) is { } map)
        {
            selected.Add(map); remaining.Remove(map);
        }
        return selected.ToImmutableArray();
    }
}
