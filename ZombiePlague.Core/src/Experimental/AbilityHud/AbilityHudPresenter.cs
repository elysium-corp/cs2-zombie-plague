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
    public int PlayerCount => _frames.Count;

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
            if (oldIcon?.Name != icon?.Name) sink.SetText(playerId, panel + "Name", icon?.Name ?? "");
            if (oldIcon?.Countdown != icon?.Countdown) sink.SetText(playerId, panel + "Time", icon?.Countdown ?? "");
            if (oldIcon?.Hotkey != icon?.Hotkey) sink.SetText(playerId, panel + "Key", icon?.Hotkey ?? "");
            if ((oldIcon is null) != (icon is null)) sink.SetClass(playerId, panel, "Shown", icon is not null);
        }
        if (previous?.Overflow != frame.Overflow)
        {
            sink.SetText(playerId, "Overflow", frame.Overflow > 0 ? "+" + frame.Overflow : "");
            sink.SetClass(playerId, "Overflow", "Shown", frame.Overflow > 0);
        }
        if (previous?.Human != frame.Human) sink.SetText(playerId, "Side", frame.Human ? "ЛЮДИ" : "ЗОМБИ");
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
