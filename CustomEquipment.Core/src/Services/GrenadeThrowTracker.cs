using CustomEquipment.Api.Data;

namespace CustomEquipment.Services;

internal sealed class GrenadeThrowTracker
{
    private const long LifetimeMilliseconds = 2000;
    private const int MaximumPendingThrows = 512;
    private readonly Dictionary<(nint Pawn, string Weapon), Entry> _pending = [];

    private sealed record Entry(GrenadeItemBase Grenade, long CapturedAt);

    internal void Capture(nint pawn, string weapon, GrenadeItemBase grenade, long now)
    {
        foreach (var key in _pending.Where(pair => now - pair.Value.CapturedAt >= LifetimeMilliseconds)
                     .Select(pair => pair.Key).ToArray())
        {
            _pending.Remove(key);
        }

        if (_pending.Count >= MaximumPendingThrows && !_pending.ContainsKey((pawn, weapon))) return;
        _pending[(pawn, weapon)] = new Entry(grenade, now);
    }

    internal GrenadeItemBase? TryTake(nint pawn, string weapon, long now)
    {
        return _pending.Remove((pawn, weapon), out var entry) &&
               now - entry.CapturedAt < LifetimeMilliseconds
            ? entry.Grenade
            : null;
    }

    internal void Clear() => _pending.Clear();
}
