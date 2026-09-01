using CustomKnife.Data.Models;

namespace CustomKnife.Data.Registrator;

internal sealed class KnivesRegistry : IKnivesRegistry, IWritableKnivesRegistry
{
    private readonly Lock _sync = new();
    private readonly Dictionary<string, IKnife> _knives = new(StringComparer.Ordinal);
    private readonly HashSet<string> _catalogKnifeIds = new(StringComparer.Ordinal);

    public IReadOnlyCollection<IKnife> GetAll()
    {
        lock (_sync)
        {
            return _knives.Values.ToArray();
        }
    }

    public bool TryGet(string knifeId, out IKnife knife)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knifeId);

        lock (_sync)
        {
            return _knives.TryGetValue(knifeId, out knife!);
        }
    }

    public bool TryRegister(IKnife knife)
    {
        ArgumentNullException.ThrowIfNull(knife);

        if (string.IsNullOrWhiteSpace(knife.InternalName))
        {
            throw new ArgumentException("Knife InternalName cannot be empty", nameof(knife));
        }

        lock (_sync)
        {
            return _knives.TryAdd(knife.InternalName, knife);
        }
    }

    public bool Unregister(string knifeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            knifeId
        );

        lock (_sync)
        {
            _catalogKnifeIds.Remove(knifeId);
            return _knives.Remove(knifeId);
        }
    }

    public void ReplaceAll(IReadOnlyCollection<IKnife> knives)
    {
        ArgumentNullException.ThrowIfNull(knives);

        var replacement = new Dictionary<string, IKnife>(StringComparer.Ordinal);

        foreach (var knife in knives)
        {
            ArgumentNullException.ThrowIfNull(knife);

            if (string.IsNullOrWhiteSpace(knife.InternalName))
            {
                throw new InvalidOperationException("Knife InternalName cannot be empty.");
            }

            if (!replacement.TryAdd(knife.InternalName, knife))
            {
                throw new InvalidOperationException($"Knife '{knife.InternalName}' is duplicated.");
            }
        }

        lock (_sync)
        {
            var externalKnives = _knives
                .Where(pair => !_catalogKnifeIds.Contains(pair.Key))
                .ToArray();

            foreach (var pair in externalKnives)
            {
                if (replacement.ContainsKey(pair.Key))
                {
                    throw new InvalidOperationException(
                        $"Catalog knife '{pair.Key}' conflicts with an externally registered knife."
                    );
                }
            }

            _knives.Clear();
            _catalogKnifeIds.Clear();

            foreach (var pair in replacement)
            {
                _knives.Add(pair.Key, pair.Value);
                _catalogKnifeIds.Add(pair.Key);
            }

            foreach (var pair in externalKnives)
            {
                _knives.Add(pair.Key, pair.Value);
            }
        }
    }
}
