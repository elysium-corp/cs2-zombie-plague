using CustomHud.Api;

namespace CustomHud.Core;

internal sealed class HudPresenter(IHudRuntime runtime, TimeProvider? clock = null, Action<int, string, float>? playSound = null)
{
    private readonly TimeProvider _clock = clock ?? TimeProvider.System;
    private readonly Dictionary<(int Player, HudPosition Position), (ulong SteamId, HudMessage Message)> _shown = [];
    private readonly Dictionary<(int Player, HudPosition Position), long> _exits = [];
    private readonly HashSet<(int Player, HudPosition Position)> _entryB = [];
    private readonly Dictionary<int, ulong> _sessions = [];
    internal int[] PlayerIds => _shown.Keys.Select(key => key.Player).Distinct().ToArray();

    internal void Render(int playerId, ulong steamId, HudPosition position, HudMessage? message, bool immediate = false)
    {
        if (_sessions.TryGetValue(playerId, out var owner) && owner != steamId) Clear(playerId);
        var key = (playerId, position);
        _shown.TryGetValue(key, out var previous);
        var old = previous.Message;
        if (old?.Revision == message?.Revision) return;
        var slot = $"Message{(int)position}";
        if (!immediate && message is null && old?.Document.Banner?.Template is { Exit: not "none" } outgoing)
        {
            if (!_exits.TryGetValue(key, out var started))
            {
                _exits[key] = _clock.GetTimestamp();
                runtime.SetClass(playerId, slot, "Leaving", true);
                return;
            }
            if (_clock.GetElapsedTime(started).TotalSeconds < HudBannerDesign.Seconds(outgoing)) return;
        }
        if (_exits.Remove(key)) runtime.SetClass(playerId, slot, "Leaving", false);
        if (message is null) runtime.SetClass(playerId, slot, "Shown", false);
        if ((old?.Options.Style == HudMessageStyle.Banner) != (message?.Options.Style == HudMessageStyle.Banner))
            runtime.SetClass(playerId, slot, "Banner", message?.Options.Style == HudMessageStyle.Banner);
        var beforeClasses = old?.Document.Banner is { } aBanner ? HudBannerDesign.Classes(aBanner.Template) : [];
        var afterClasses = message?.Document.Banner is { } bBanner ? HudBannerDesign.Classes(bBanner.Template) : [];
        foreach (var name in beforeClasses.Except(afterClasses)) runtime.SetClass(playerId, slot, name, false);
        foreach (var name in afterClasses.Except(beforeClasses)) runtime.SetClass(playerId, slot, name, true);
        Row(playerId, slot + "Header", old?.Document.Banner?.Header ?? [], message?.Document.Banner?.Header ?? []);
        Row(playerId, slot + "Title", old?.Document.Banner?.Title ?? [], message?.Document.Banner?.Title ?? []);
        for (var line = 0; line < HudMarkup.MaximumLines; line++)
        {
            var before = old is not null && line < old.Document.Lines.Length ? old.Document.Lines[line] : [];
            var after = message is not null && line < message.Document.Lines.Length ? message.Document.Lines[line] : [];
            Row(playerId, slot + "Line" + line, before, after,
                old is not null && line < old.Document.Lines.Length, message is not null && line < message.Document.Lines.Length);
        }
        if (message is null)
        {
            _shown.Remove(key);
            runtime.SetClass(playerId, slot, _entryB.Remove(key) ? "EntryB" : "EntryA", false);
        }
        else
        {
            // Разные имена keyframes перезапускают появление даже при замене в том же сетевом кадре.
            var wasB = _entryB.Contains(key);
            runtime.SetClass(playerId, slot, wasB ? "EntryB" : "EntryA", false);
            runtime.SetClass(playerId, slot, wasB ? "EntryA" : "EntryB", true);
            if (wasB) _entryB.Remove(key); else _entryB.Add(key);
            if (old is null) runtime.SetClass(playerId, slot, "Shown", true);
            _sessions[playerId] = steamId;
            _shown[key] = (steamId, message);
            if (!message.SoundPlayed && message.Document.Banner?.Template is { Sound: { } sound, Volume: > 0 } template)
            {
                message.SoundPlayed = true;
                playSound?.Invoke(playerId, sound, template.Volume);
            }
        }
    }

    private void Row(int playerId, string row, HudRun[] before, HudRun[] after, bool? wasVisible = null, bool? visible = null)
    {
        wasVisible ??= before.Length > 0;
        visible ??= after.Length > 0;
        if (wasVisible != visible) runtime.SetClass(playerId, row, "Shown", visible.Value);
        for (var run = 0; run < Math.Max(before.Length, after.Length); run++)
        {
            var panel = row + "Run" + run;
            var a = run < before.Length ? before[run] : null;
            var b = run < after.Length ? after[run] : null;
            if (a == b) continue;
            var oldStyle = a?.Style ?? HudRunStyle.Default;
            var newStyle = b?.Style ?? HudRunStyle.Default;
            if (oldStyle.Color != newStyle.Color || oldStyle.ExplicitColor != newStyle.ExplicitColor)
            {
                if (oldStyle.Color != HudPalette.White || oldStyle.ExplicitColor) runtime.SetClass(playerId, panel, "C" + oldStyle.Color, false);
                if (newStyle.Color != HudPalette.White || newStyle.ExplicitColor) runtime.SetClass(playerId, panel, "C" + newStyle.Color, true);
            }
            if (oldStyle.Bold != newStyle.Bold) runtime.SetClass(playerId, panel, "Bold", newStyle.Bold);
            if (oldStyle.Italic != newStyle.Italic) runtime.SetClass(playerId, panel, "Italic", newStyle.Italic);
            if (oldStyle.Underline != newStyle.Underline) runtime.SetClass(playerId, panel, "Underline", newStyle.Underline);
            if (a?.Text != b?.Text) runtime.SetText(playerId, panel, b?.Text.Replace(' ', '\u00a0') ?? string.Empty);
            if ((a is null) != (b is null)) runtime.SetClass(playerId, panel, "Shown", b is not null);
        }
    }

    internal void Clear(int playerId)
    {
        foreach (var key in _shown.Keys.Where(key => key.Player == playerId).ToArray())
            Render(playerId, _shown[key].SteamId, key.Position, null, immediate: true);
        _sessions.Remove(playerId);
    }
}
