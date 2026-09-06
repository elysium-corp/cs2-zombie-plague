namespace ZombiePlague.Core.Experimental.AbilityHud;

internal interface IAbilityHudSink
{
    void SetClass(int playerId, string panel, string name, bool enabled);
    void SetText(int playerId, string panel, string value);
}

internal sealed class AbilityHudPresenter(IAbilityHudSink sink)
{
    private readonly Dictionary<int, AbilityHudFrame> _frames = [];
    public IEnumerable<int> PlayerIds => _frames.Keys;
    public int PlayerCount => _frames.Values.Count(frame => frame.Icons.Length > 0);

    public void Render(int playerId, AbilityHudFrame frame)
    {
        _frames.TryGetValue(playerId, out var previous);
        for (var slot = 0; slot < Math.Max(previous?.Icons.Length ?? 0, frame.Icons.Length); slot++)
        {
            var oldIcon = previous?.Icons.ElementAtOrDefault(slot);
            var icon = frame.Icons.ElementAtOrDefault(slot);
            if (oldIcon == icon) continue;
            var panel = "Buff" + slot;
            if (oldIcon?.Kind != icon?.Kind)
            {
                if (oldIcon is not null) sink.SetClass(playerId, panel, "Kind_" + oldIcon.Kind, false);
                if (icon is not null) sink.SetClass(playerId, panel, "Kind_" + icon.Kind, true);
            }
            if (oldIcon?.State != icon?.State)
            {
                if (oldIcon is not null) sink.SetClass(playerId, panel, oldIcon.State, false);
                if (icon is not null) sink.SetClass(playerId, panel, icon.State, true);
            }
            if ((oldIcon?.Passive ?? false) != (icon?.Passive ?? false))
                sink.SetClass(playerId, panel, "Passive", icon?.Passive ?? false);
            if (oldIcon?.Name != icon?.Name) sink.SetText(playerId, panel + "Name", icon?.Name ?? "");
            if (oldIcon?.Countdown != icon?.Countdown) sink.SetText(playerId, panel + "Time", icon?.Countdown ?? "");
            if (oldIcon?.Hotkey != icon?.Hotkey) sink.SetText(playerId, panel + "Key", icon?.Hotkey ?? "");
            if ((oldIcon is null) != (icon is null)) sink.SetClass(playerId, panel, "Shown", icon is not null);
        }
        for (var row = 0; row < AbilityHudFrame.MaximumRows; row++)
        {
            if ((row < (previous?.RowCount ?? 0)) != (row < frame.RowCount))
                sink.SetClass(playerId, "BuffRow" + row, "Shown", row < frame.RowCount);
        }
        if (previous?.ShowNames != frame.ShowNames) sink.SetClass(playerId, "AbilityBuffs", "ShowNames", frame.ShowNames);
        // Показ корня идёт последним, после заполнения всех иконок персонального набора
        if (previous is null || (previous.Icons.Length > 0) != (frame.Icons.Length > 0))
            sink.SetClass(playerId, "AbilityBuffs", "Shown", frame.Icons.Length > 0);
        _frames[playerId] = frame;
    }

    public void Clear(int playerId)
    {
        Render(playerId, AbilityHudFrame.Empty);
        _frames.Remove(playerId);
    }
}
