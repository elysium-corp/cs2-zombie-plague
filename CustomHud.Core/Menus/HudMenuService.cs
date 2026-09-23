using CustomHud.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace CustomHud.Core.Menus;

internal sealed class HudMenuService(ISwiftlyCore core, Func<int, IHudMenuRuntime> createRuntime) : ICustomHudMenuApi, IDisposable
{
    private readonly Dictionary<int, Session> _sessions = [];
    private CancellationTokenSource? _timer;
    private bool _started;
    private bool _unloading;
    private bool _disposed;
    public event Action<IPlayer>? Opening;
    public bool IsAnyOpen(IPlayer player) => Eligible(player) && _sessions.TryGetValue(player.PlayerID, out var session)
        && SamePlayer(player, session);

    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        core.Event.OnCustomHudClicked += OnClick;
        core.Event.OnClientKeyStateChanged += OnKey;
        core.Event.OnClientConnected += OnConnect;
        core.Event.OnClientDisconnected += OnDisconnect;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnMapLoad += OnMapLoad;
        _timer = core.Scheduler.RepeatBySeconds(1, Sweep);
    }

    public Guid? Open(IPlayer player, HudMenu menu, Action<HudMenuEvent> onAction)
    {
        if (!Eligible(player) || !Valid(menu)) return null;
        if (_sessions.TryGetValue(player.PlayerID, out var current) && SamePlayer(player, current)
            && current.Menu.Options.Priority > menu.Options.Priority) return null;
        ClosePlayer(player.PlayerID);
        var session = new Session(player, menu, onAction);
        _sessions[player.PlayerID] = session;
        try { Opening?.Invoke(player); Render(session); return session.Id; }
        catch (Exception error) { Fail(player.PlayerID, error); return null; }
    }

    public bool Update(IPlayer player, Guid menuId, HudMenu menu)
    {
        if (!Eligible(player) || !Valid(menu) || !TrySession(player, menuId, out var session)
            || session.Menu.Channel != menu.Channel || session.Menu.Options.Priority != menu.Options.Priority) return false;
        var changedSlots = session.Menu.Options.ItemsPerPage != menu.Options.ItemsPerPage
            || !session.Menu.Items.Select(item => item.Id).SequenceEqual(menu.Items.Select(item => item.Id));
        session.Menu = menu;
        session.Page = Math.Min(session.Page, PageCount(menu) - 1);
        try
        {
            if (changedSlots) ReplaceRuntime(session);
            Render(session); return true;
        }
        catch (Exception error) { Fail(player.PlayerID, error); return false; }
    }

    public void Close(IPlayer player, Guid menuId)
    {
        if (TrySession(player, menuId, out _)) ClosePlayer(player.PlayerID);
    }
    public bool IsOpen(IPlayer player, Guid menuId) => Eligible(player) && TrySession(player, menuId, out var session)
        && session.Runtime?.IsValid == true;
    public void CloseChannel(string channel)
    {
        foreach (var session in _sessions.Values.Where(session => session.Menu.Channel == channel).ToArray())
            ClosePlayer(session.PlayerId);
    }

    private bool Eligible(IPlayer player) => _started && !_disposed && !_unloading && player.IsValid && !player.IsFakeClient && player.SteamID != 0;
    private static bool SamePlayer(IPlayer player, Session session) => player.SessionId == session.ConnectionId && player.SteamID == session.SteamId;
    private bool TrySession(IPlayer player, Guid id, out Session session)
    {
        if (_sessions.TryGetValue(player.PlayerID, out session!) && session.Id == id && SamePlayer(player, session)) return true;
        session = null!; return false;
    }
    private static bool Valid(HudMenu menu) => !string.IsNullOrWhiteSpace(menu.Channel) && menu.Channel.Length <= 64
        && menu.Options.ItemsPerPage is >= 1 and <= 6 && !menu.Items.IsDefault
        && menu.Items.Length <= 1000 && menu.Items.All(item => !string.IsNullOrEmpty(item.Id))
        && menu.Items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == menu.Items.Length;
    private static int PageCount(HudMenu menu) => Math.Max(1, (menu.Items.Length + menu.Options.ItemsPerPage - 1) / menu.Options.ItemsPerPage);

    private void Render(Session session)
    {
        var menu = session.Menu;
        var hud = session.Runtime ??= createRuntime(session.PlayerId);
        if (!hud.IsValid) throw new InvalidOperationException("Сущность HUD-меню удалена");
        hud.Text("Title", menu.Title); hud.Text("Subtitle", menu.Subtitle);
        hud.Text("Status", menu.Status); hud.Text("CloseText", menu.CloseText); hud.Text("Footer", menu.Footer);
        hud.Class("MenuRoot", "Modal", menu.Options.Modal);
        hud.Class("MenuRoot", "Result", menu.View == HudMenuView.Result);
        hud.Class("Close", "Hidden", !menu.Options.Closable);
        hud.Class("CloseHint", "Hidden", !menu.Options.Closable);
        hud.Class("Back", "Hidden", !menu.ShowBack);
        hud.Class("Status", "Hidden", string.IsNullOrEmpty(menu.Status));
        hud.Class("Footer", "Hidden", string.IsNullOrEmpty(menu.Footer));
        var pages = PageCount(menu);
        hud.Text("Page", $"{session.Page + 1} / {pages}");
        hud.Class("Pagination", "Hidden", !menu.Options.ShowPagination || pages == 1);
        hud.Class("PreviousPage", "Disabled", session.Page == 0);
        hud.Class("NextPage", "Disabled", session.Page + 1 >= pages);
        for (var slot = 0; slot < 6; slot++)
        {
            var index = session.Page * menu.Options.ItemsPerPage + slot;
            var item = slot < menu.Options.ItemsPerPage && index < menu.Items.Length ? menu.Items[index] : null;
            hud.Class("Item" + slot, "Hidden", item is null);
            if (item is null) continue;
            hud.Text("Name" + slot, item.Title);
            hud.Text("Description" + slot, item.Enabled ? item.Description : item.DisabledReason);
            hud.Class("Description" + slot, "Hidden", string.IsNullOrEmpty(item.Enabled ? item.Description : item.DisabledReason));
            hud.Text("Badge" + slot, item.Badge);
            hud.Class("Item" + slot, "Disabled", !item.Enabled);
            hud.Class("Item" + slot, "Selected", item.Selected);
        }
        hud.Show(menu.Options.CaptureInput);
    }

    private void OnClick(IOnCustomHudClickedEvent args)
    {
        if (!_started || _disposed || _unloading || !_sessions.TryGetValue(args.PlayerId, out var session)
            || session.Runtime?.Owns(args.CustomHudLayout) != true
            || core.PlayerManager.GetPlayer(args.PlayerId) is not { } player || !Eligible(player) || !SamePlayer(player, session)) return;
        Route(session, args.ButtonId);
    }

    private void Route(Session session, string button)
    {
        try
        {
            HudMenuAction action;
            string? itemId = null;
            if (button == "Close")
            {
                if (!session.Menu.Options.Closable) return;
                action = HudMenuAction.Close;
                ClosePlayer(session.PlayerId);
            }
            else if (button == "Back")
            {
                if (!session.Menu.ShowBack) return;
                action = HudMenuAction.Back;
            }
            else if (button is "NextPage" or "PreviousPage")
            {
                if (!session.Menu.Options.ShowPagination) return;
                var next = session.Page + (button == "NextPage" ? 1 : -1);
                if (next < 0 || next >= PageCount(session.Menu)) return;
                session.Page = next;
                // Новая сущность исключает отложенный клик по старой странице.
                ReplaceRuntime(session); Render(session);
                action = button == "NextPage" ? HudMenuAction.NextPage : HudMenuAction.PreviousPage;
            }
            else if (button.Length == 5 && button.StartsWith("Item", StringComparison.Ordinal) && button[4] is >= '0' and <= '5')
            {
                var slot = button[4] - '0';
                var index = session.Page * session.Menu.Options.ItemsPerPage + slot;
                if (slot >= session.Menu.Options.ItemsPerPage || index >= session.Menu.Items.Length) return;
                var item = session.Menu.Items[index];
                if (!item.Enabled) return;
                action = HudMenuAction.Select; itemId = item.Id;
                if (session.Menu.Options.CloseOnSelect) ClosePlayer(session.PlayerId);
            }
            else return;
            session.Callback(new(session.Id, action, itemId));
        }
        catch (Exception error) { Fail(session.PlayerId, error); }
    }

    private static void ReplaceRuntime(Session session)
    {
        var old = session.Runtime; session.Runtime = null; old?.Dispose();
    }
    private void ClosePlayer(int id)
    {
        if (!_sessions.Remove(id, out var session)) return;
        try { session.Runtime?.Dispose(); }
        catch (Exception error) { core.Logger.LogWarning(error, "[HUD Menu] Ошибка освобождения ввода игрока {PlayerId}", id); }
    }
    private void Sweep()
    {
        if (_disposed || _unloading) return;
        foreach (var (id, session) in _sessions.ToArray())
        {
            var player = core.PlayerManager.GetPlayer(id);
            if (player is null || !Eligible(player) || !SamePlayer(player, session) || session.Runtime?.IsValid != true) ClosePlayer(id);
        }
    }
    private void OnKey(IOnClientKeyStateChangedEvent args)
    {
        if (args.Key == KeyKind.Esc && args.Pressed && _sessions.TryGetValue(args.PlayerId, out var session)
            && core.PlayerManager.GetPlayer(args.PlayerId) is { } player && SamePlayer(player, session)) Route(session, "Close");
    }
    private void OnConnect(IOnClientConnectedEvent args) => ClosePlayer(args.PlayerId);
    private void OnDisconnect(IOnClientDisconnectedEvent args) => ClosePlayer(args.PlayerId);
    private void OnMapUnload(IOnMapUnloadEvent args) { _unloading = true; CloseAll(); }
    private void OnMapLoad(IOnMapLoadEvent args) { CloseAll(); _unloading = false; }
    private void CloseAll() { foreach (var id in _sessions.Keys.ToArray()) ClosePlayer(id); }
    private void Fail(int id, Exception error)
    {
        ClosePlayer(id);
        core.Logger.LogError(error, "[HUD Menu] Меню игрока {PlayerId} закрыто после ошибки", id);
    }
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _timer?.Cancel(); _timer = null;
        if (_started)
        {
            core.Event.OnCustomHudClicked -= OnClick;
            core.Event.OnClientKeyStateChanged -= OnKey;
            core.Event.OnClientConnected -= OnConnect;
            core.Event.OnClientDisconnected -= OnDisconnect;
            core.Event.OnMapUnload -= OnMapUnload;
            core.Event.OnMapLoad -= OnMapLoad;
        }
        CloseAll();
    }

    private sealed class Session(IPlayer player, HudMenu menu, Action<HudMenuEvent> callback)
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int PlayerId { get; } = player.PlayerID;
        public ulong ConnectionId { get; } = player.SessionId;
        public ulong SteamId { get; } = player.SteamID;
        public HudMenu Menu { get; set; } = menu;
        public Action<HudMenuEvent> Callback { get; } = callback;
        public IHudMenuRuntime? Runtime { get; set; }
        public int Page { get; set; }
    }
}
