using CustomHud.Api;

namespace CustomHud.Core;

internal sealed record HudMessage(long Revision, HudMessageOptions Options, HudDocument Document, long CreatedAt)
{
    internal bool SoundPlayed { get; set; }
}

internal sealed class HudMessageStore(TimeProvider clock)
{
    internal const int MaximumMessagesPerPlayer = 32;
    private sealed record Session(ulong SteamId, Dictionary<(string Channel, HudPosition Position), HudMessage> Messages);
    private readonly Dictionary<int, Session> _players = [];
    private long _revision;

    internal bool Put(int playerId, ulong steamId, HudDocument document, HudMessageOptions options)
    {
        if (!_players.TryGetValue(playerId, out var session) || session.SteamId != steamId)
            _players[playerId] = session = new Session(steamId, []);
        Expire(session);
        var key = (options.Channel, options.Position);
        if (!session.Messages.ContainsKey(key) && session.Messages.Count >= MaximumMessagesPerPlayer) return false;
        session.Messages[key] = new HudMessage(++_revision, options, document, clock.GetTimestamp());
        return true;
    }

    internal HudMessage?[] GetFrame(int playerId, ulong steamId)
    {
        var frame = new HudMessage?[9];
        if (!_players.TryGetValue(playerId, out var session)) return frame;
        if (session.SteamId != steamId) { _players.Remove(playerId); return frame; }
        Expire(session);
        foreach (var message in session.Messages.Values)
        {
            var position = (int)message.Options.Position;
            var previous = frame[position];
            if (previous is null || message.Options.Priority > previous.Options.Priority
                || (message.Options.Priority == previous.Options.Priority && message.Revision > previous.Revision))
                frame[position] = message;
        }
        return frame;
    }

    private void Expire(Session session)
    {
        var now = clock.GetTimestamp();
        foreach (var (key, value) in session.Messages.ToArray())
            if (clock.GetElapsedTime(value.CreatedAt, now).TotalSeconds >= value.Options.DurationSeconds)
                session.Messages.Remove(key);
    }

    internal void Hide(int playerId, ulong steamId, string channel)
    {
        if (_players.TryGetValue(playerId, out var session) && session.SteamId == steamId) RemoveChannel(session, channel);
    }

    internal void ClearChannel(string channel)
    {
        foreach (var session in _players.Values) RemoveChannel(session, channel);
    }

    private static void RemoveChannel(Session session, string channel)
    {
        foreach (var key in session.Messages.Keys.Where(key => key.Channel == channel).ToArray()) session.Messages.Remove(key);
    }

    internal void Disconnect(int playerId) => _players.Remove(playerId);
    internal void Clear() => _players.Clear();

    internal static void Validate(HudMessageOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Channel) || options.Channel.Length > 64)
            throw new ArgumentException("HUD: канал должен содержать 1–64 символа", nameof(options));
        if (!Enum.IsDefined(options.Position) || !Enum.IsDefined(options.Style) || !Enum.IsDefined(options.Format))
            throw new ArgumentException("HUD: неизвестная позиция, стиль или формат", nameof(options));
        if (!double.IsFinite(options.DurationSeconds) || options.DurationSeconds is < 0.5 or > 60)
            throw new ArgumentException("HUD: время показа должно быть от 0,5 до 60 секунд", nameof(options));
        if (options.Priority is < 0 or > 1000)
            throw new ArgumentException("HUD: приоритет должен быть от 0 до 1000", nameof(options));
    }
}
