using CustomKnife.Data.Models;

namespace CustomKnife.Data.Registrator;

internal sealed class KnivesRegistry : IKnivesRegistry
{
    private readonly Dictionary<string, IKnife> _knives = new(StringComparer.Ordinal);

    public IReadOnlyCollection<IKnife> GetAll()
    {
        return _knives.Values;
    }

    public bool TryGet(string knifeId, out IKnife knife)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(knifeId);

        return _knives.TryGetValue(knifeId, out knife!);
    }

    public bool TryRegister(IKnife knife)
    {
        ArgumentNullException.ThrowIfNull(knife);

        if (string.IsNullOrWhiteSpace(knife.InternalName))
        {
            throw new ArgumentException("Knife InternalName cannot be empty", nameof(knife));
        }

        return _knives.TryAdd(knife.InternalName, knife);
    }

    public bool Unregister(string knifeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            knifeId
        );

        return _knives.Remove(knifeId);
    }
}