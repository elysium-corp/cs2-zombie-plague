using CustomHud.Api;

namespace CustomHud.Core;

internal sealed class HudPresenter(IHudRuntime runtime)
{
    private readonly Dictionary<(int Player, HudPosition Position), (ulong SteamId, HudMessage Message)> _shown = [];
    private readonly Dictionary<int, ulong> _sessions = [];
    internal int[] PlayerIds => _shown.Keys.Select(key => key.Player).Distinct().ToArray();

    internal void Render(int playerId, ulong steamId, HudPosition position, HudMessage? message)
    {
        if (_sessions.TryGetValue(playerId, out var owner) && owner != steamId) Clear(playerId);
        var key = (playerId, position);
        _shown.TryGetValue(key, out var previous);
        var old = previous.Message;
        if (old?.Revision == message?.Revision) return;
        var slot = $"Message{(int)position}";
        if (message is null) runtime.SetClass(playerId, slot, "Shown", false);
        if ((old?.Options.Style == HudMessageStyle.Banner) != (message?.Options.Style == HudMessageStyle.Banner))
            runtime.SetClass(playerId, slot, "Banner", message?.Options.Style == HudMessageStyle.Banner);
        for (var line = 0; line < HudMarkup.MaximumLines; line++)
        {
            var row = slot + "Line" + line;
            var before = old is not null && line < old.Document.Lines.Length ? old.Document.Lines[line] : [];
            var after = message is not null && line < message.Document.Lines.Length ? message.Document.Lines[line] : [];
            var wasVisible = old is not null && line < old.Document.Lines.Length;
            var visible = message is not null && line < message.Document.Lines.Length;
            if (wasVisible != visible) runtime.SetClass(playerId, row, "Shown", visible);
            for (var run = 0; run < Math.Max(before.Length, after.Length); run++)
            {
                var panel = row + "Run" + run;
                var a = run < before.Length ? before[run] : null;
                var b = run < after.Length ? after[run] : null;
                if (a == b) continue;
                var oldStyle = a?.Style ?? HudRunStyle.Default;
                var newStyle = b?.Style ?? HudRunStyle.Default;
                if (oldStyle.Color != newStyle.Color)
                {
                    if (oldStyle.Color != HudPalette.White) runtime.SetClass(playerId, panel, "C" + oldStyle.Color, false);
                    if (newStyle.Color != HudPalette.White) runtime.SetClass(playerId, panel, "C" + newStyle.Color, true);
                }
                if (oldStyle.Bold != newStyle.Bold) runtime.SetClass(playerId, panel, "Bold", newStyle.Bold);
                if (oldStyle.Italic != newStyle.Italic) runtime.SetClass(playerId, panel, "Italic", newStyle.Italic);
                if (oldStyle.Underline != newStyle.Underline) runtime.SetClass(playerId, panel, "Underline", newStyle.Underline);
                if (a?.Text != b?.Text) runtime.SetText(playerId, panel, b?.Text.Replace(' ', '\u00a0') ?? string.Empty);
                if ((a is null) != (b is null)) runtime.SetClass(playerId, panel, "Shown", b is not null);
            }
        }
        if (message is null) _shown.Remove(key);
        else
        {
            if (old is null) runtime.SetClass(playerId, slot, "Shown", true);
            _sessions[playerId] = steamId;
            _shown[key] = (steamId, message);
        }
    }

    internal void Clear(int playerId)
    {
        foreach (var key in _shown.Keys.Where(key => key.Player == playerId).ToArray())
            Render(playerId, _shown[key].SteamId, key.Position, null);
        _sessions.Remove(playerId);
    }
}
