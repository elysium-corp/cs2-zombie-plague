using CustomHud.Api;

namespace Advertisement.Core.Application;

// Очередь хранит данные события, а не IPlayer: повторное использование слота не доставляет чужие сообщения.
internal sealed class NotificationQueue(TimeProvider clock)
{
    internal sealed record Pending(int PlayerId, ulong SteamId, BannerNotificationRule Rule,
        IReadOnlyDictionary<string, object?> Parameters, DateTimeOffset CreatedAt)
    {
        internal bool IsUpdate { get; init; }
        internal string? PreviousEvent { get; init; }
    }
    private sealed class Slot
    {
        internal readonly List<Pending> Waiting = [];
        internal Pending? Active;
        internal DateTimeOffset Until;
    }
    private readonly Dictionary<(int Player, HudPosition Position), Slot> _slots = [];
    private readonly Dictionary<int, ulong> _sessions = [];
    private readonly Dictionary<(int Player, string Event), DateTimeOffset> _lastAccepted = [];
    internal IEnumerable<string> EventKeys => _slots.Values.SelectMany(slot => slot.Waiting.Select(item => item.Rule.EventKey)
        .Concat(slot.Active is { } active ? [active.Rule.EventKey] : [])).Distinct().ToArray();

    internal bool Enqueue(int playerId, ulong steamId, BannerNotificationRule rule, IReadOnlyDictionary<string, object?> parameters)
    {
        var now = clock.GetUtcNow();
        if (_sessions.TryGetValue(playerId, out var previous) && previous != steamId) Disconnect(playerId);
        _sessions[playerId] = steamId;
        var eventId = (playerId, rule.EventKey);
        if (_lastAccepted.TryGetValue(eventId, out var last) && (now - last).TotalSeconds < rule.CooldownSeconds) return false;
        var key = (playerId, rule.Options.Position);
        if (!_slots.TryGetValue(key, out var slot)) _slots[key] = slot = new();
        if (rule.Delivery == "replace") slot.Waiting.RemoveAll(item => item.Rule.EventKey == rule.EventKey);
        var waiting = _slots.Where(pair => pair.Key.Player == playerId).SelectMany(pair => pair.Value.Waiting).ToArray();
        if (waiting.Length >= 32)
        {
            var victim = waiting.OrderBy(item => item.Rule.Options.Priority).ThenBy(item => item.CreatedAt).First();
            if (victim.Rule.Options.Priority > rule.Options.Priority) return false;
            _slots[(playerId, victim.Rule.Options.Position)].Waiting.Remove(victim);
        }
        slot.Waiting.Add(new(playerId, steamId, rule, new Dictionary<string, object?>(parameters, StringComparer.OrdinalIgnoreCase), now));
        _lastAccepted[eventId] = now;
        return true;
    }

    internal Pending[] Ready()
    {
        var now = clock.GetUtcNow();
        var result = new List<Pending>();
        foreach (var slot in _slots.Values)
        {
            slot.Waiting.RemoveAll(item => (now - item.CreatedAt).TotalSeconds > item.Rule.MaxQueueAgeSeconds);
            var candidate = slot.Waiting.OrderByDescending(item => item.Rule.Options.Priority).FirstOrDefault();
            if (candidate is null) continue;
            var active = slot.Until > now ? slot.Active : null;
            var update = active?.Rule.EventKey == candidate.Rule.EventKey && candidate.Rule.Delivery == "replace";
            if (active is not null && !update && candidate.Rule.Options.Priority <= active.Rule.Options.Priority) continue;
            slot.Waiting.Remove(candidate);
            result.Add(candidate with { IsUpdate = update, PreviousEvent = active?.Rule.EventKey });
        }
        return result.ToArray();
    }

    internal void Shown(Pending pending)
    {
        if (!_slots.TryGetValue((pending.PlayerId, pending.Rule.Options.Position), out var slot)) return;
        slot.Active = pending;
        var exit = pending.Rule.Template.Exit == "none" ? 0 : pending.Rule.Template.Speed switch { "fast" => .2, "slow" => .8, _ => .4 };
        slot.Until = clock.GetUtcNow().AddSeconds(pending.Rule.Options.DurationSeconds + exit);
    }
    internal void Reject(Pending pending) { /* Не блокируем область после отказа Localization или HUD. */ }
    internal void ClearEvent(string eventKey)
    {
        foreach (var slot in _slots.Values)
        {
            slot.Waiting.RemoveAll(item => item.Rule.EventKey == eventKey);
            if (slot.Active?.Rule.EventKey == eventKey) { slot.Active = null; slot.Until = default; }
        }
        foreach (var key in _lastAccepted.Keys.Where(key => key.Event == eventKey).ToArray()) _lastAccepted.Remove(key);
    }
    internal void Disconnect(int playerId)
    {
        foreach (var key in _slots.Keys.Where(key => key.Player == playerId).ToArray()) _slots.Remove(key);
        foreach (var key in _lastAccepted.Keys.Where(key => key.Player == playerId).ToArray()) _lastAccepted.Remove(key);
        _sessions.Remove(playerId);
    }
    internal void Reset() { _slots.Clear(); _sessions.Clear(); _lastAccepted.Clear(); }
}
