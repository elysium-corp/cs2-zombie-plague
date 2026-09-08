using CustomHud.Api;

namespace CustomHud.Core;

internal sealed record HudMessage(long Revision, HudMessageOptions Options, HudDocument Document, long CreatedAt)
{
    internal bool Presented { get; set; }
    internal bool SoundPlayed { get; set; }
}

internal sealed class HudMessageStore(TimeProvider clock)
{
    internal const int MaximumMessagesPerPlayer = 32;
    private sealed record Session(ulong SteamId, Dictionary<(string Channel, HudPosition Position, long Instance), HudMessage> Messages);
    private readonly Dictionary<int, Session> _players = [];
    private long _revision;

    internal bool Put(int playerId, ulong steamId, HudDocument document, HudMessageOptions options)
    {
        if (!_players.TryGetValue(playerId, out var session) || session.SteamId != steamId)
            _players[playerId] = session = new Session(steamId, []);
        Expire(session, includeExit: true);
        var region = session.Messages.Values.Where(message => message.Options.Position == options.Position).ToArray();
        var stacks = region.Count(message => message.Options.Stack);
        if (options.Stack ? stacks + (region.Any(message => !message.Options.Stack) ? 1 : 0) >= PanoramaHudRuntime.StackCapacity
            : stacks >= PanoramaHudRuntime.StackCapacity) return false;
        var key = (options.Channel, options.Position, options.Stack ? _revision + 1 : 0);
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

    internal HudMessage?[] GetStackedFrame(int playerId, ulong steamId)
    {
        var frame = new HudMessage?[PanoramaHudRuntime.RegionCount * PanoramaHudRuntime.StackCapacity];
        if (!_players.TryGetValue(playerId, out var session)) return frame;
        if (session.SteamId != steamId) { _players.Remove(playerId); return frame; }
        Expire(session, includeExit: true);
        foreach (var region in session.Messages.Values.GroupBy(message => message.Options.Position))
        {
            var normal = region.Where(message => !message.Options.Stack).OrderByDescending(message => message.Options.Priority)
                .ThenByDescending(message => message.Revision).FirstOrDefault();
            var visible = region.Where(message => message.Options.Stack).OrderBy(message => message.Revision).ToList();
            if (normal is not null) visible.Insert(0, normal);
            for (var lane = 0; lane < Math.Min(visible.Count, PanoramaHudRuntime.StackCapacity); lane++)
                frame[(int)region.Key + lane * PanoramaHudRuntime.RegionCount] = visible[lane];
        }
        return frame;
    }

    private void Expire(Session session, bool includeExit = false)
    {
        var now = clock.GetTimestamp();
        foreach (var (key, value) in session.Messages.ToArray())
            if (clock.GetElapsedTime(value.CreatedAt, now).TotalSeconds >= value.Options.DurationSeconds + (includeExit && value.Document.Banner?.Template is { Exit: not "none" } template ? HudBannerDesign.Seconds(template) : 0))
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
