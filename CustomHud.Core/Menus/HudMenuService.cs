using Common.Di.Diagnostics;
using CustomHud.Api;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace CustomHud.Core.Menus;

internal sealed class HudMenuService(ISwiftlyCore core, Func<int, IHudMenuRuntime> createRuntime) : ICustomHudMenuApi, IDisposable
{
    private const int SlotCount = 12;
    private readonly Dictionary<int, Session> _sessions = [];
    private CancellationTokenSource? _timer;
    private bool _started;
    private bool _unloading;
    private bool _disposed;
    public event Action<IPlayer>? Opening;
    public bool IsAnyOpen(IPlayer player) => Eligible(player) && _sessions.TryGetValue(player.PlayerID, out var session)
        && SamePlayer(player, session) && Captures(session.Menu);

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
            && Captures(current.Menu) && current.Menu.Options.Priority > menu.Options.Priority) return null;
        ClosePlayer(player.PlayerID);
        var session = new Session(player, menu, onAction);
        _sessions[player.PlayerID] = session;
        try
        {
            if (Captures(menu)) Opening?.Invoke(player);
            session.Surface = new(createRuntime(session.PlayerId));
            session.Surface.Runtime.Class("MenuRoot", "EntranceAnimated", true);
            Render(session, session.Surface);
            return session.Id;
        }
        catch (Exception error) { Fail(player.PlayerID, error); return null; }
    }

    public bool Update(IPlayer player, Guid menuId, HudMenu menu)
    {
        if (!Eligible(player) || !Valid(menu) || !TrySession(player, menuId, out var session)
            || session.Menu.Channel != menu.Channel || session.Menu.Options.Priority != menu.Options.Priority) return false;
        var changedSlots = !SameBindings(session.Menu, menu, session.Page);
        var previousView = session.Menu.View;
        var acquiredInput = !Captures(session.Menu) && Captures(menu);
        session.Menu = menu;
        session.Page = Math.Min(session.Page, PageCount(menu) - 1);
        try
        {
            if (acquiredInput) Opening?.Invoke(player);
            // Пассивный результат не принимает выбор, поэтому смена его содержимого не требует новой сущности.
            if (changedSlots && menu.View == HudMenuView.List) StagePage(session, 1);
            else if (previousView != menu.View)
            {
                CancelTransition(session);
                if (previousView == HudMenuView.List && menu.View == HudMenuView.Compact) Minimize(session);
                else Render(session, session.Surface!);
            }
            else if (session.Staged is { } staged) Render(session, staged, show: false);
            else if (!session.Transitioning) Render(session, session.Surface!);
            return true;
        }
        catch (Exception error) { Fail(player.PlayerID, error); return false; }
    }

    public void Close(IPlayer player, Guid menuId)
    {
        if (TrySession(player, menuId, out _)) ClosePlayer(player.PlayerID);
    }
    public bool IsOpen(IPlayer player, Guid menuId) => Eligible(player) && TrySession(player, menuId, out var session)
        && session.Surface?.Runtime.IsValid == true;
    public void CloseChannel(string channel)
    {
        foreach (var session in _sessions.Values.Where(session => session.Menu.Channel == channel).ToArray())
            ClosePlayer(session.PlayerId);
    }

    private static bool Captures(HudMenu menu) => menu.View == HudMenuView.List && menu.Options.CaptureInput;
    private bool Eligible(IPlayer player) => _started && !_disposed && !_unloading && player.IsValid && !player.IsFakeClient && player.SteamID != 0;
    private static bool SamePlayer(IPlayer player, Session session) => player.SessionId == session.ConnectionId && player.SteamID == session.SteamId;
    private bool TrySession(IPlayer player, Guid id, out Session session)
    {
        if (_sessions.TryGetValue(player.PlayerID, out session!) && session.Id == id && SamePlayer(player, session)) return true;
        session = null!; return false;
    }
    private static bool Valid(HudMenu menu) => !string.IsNullOrWhiteSpace(menu.Channel) && menu.Channel.Length <= 64
        && (menu.StyleClass.Length == 0 || Regex.IsMatch(menu.StyleClass, "\\A[A-Za-z][A-Za-z0-9_]{0,63}\\z"))
        && menu.Options.ItemsPerPage is >= 1 and <= SlotCount && !menu.Items.IsDefault
        && Enum.IsDefined(menu.View) && Enum.IsDefined(menu.Presentation.Orientation)
        && Enum.IsDefined(menu.Presentation.DockSide) && Enum.IsDefined(menu.Presentation.Animation)
        && menu.Presentation.ScalePercent is 80 or 100 or 120
        && menu.VerticalGap is null or >= 0 and <= 32
        && menu.Items.Length <= 1000 && menu.Items.All(item => !string.IsNullOrEmpty(item.Id) && item.Percent is null or >= 0 and <= 100)
        && menu.Items.All(item => item.ImagePath is null || Regex.IsMatch(item.ImagePath,
            "\\Apanorama/images/custom_game/elysium/assets/[a-f0-9]{64}_png\\.vtex\\z"))
        && menu.Items.Select(item => item.Id).Distinct(StringComparer.Ordinal).Count() == menu.Items.Length;
    private static int PageCount(HudMenu menu) => Math.Max(1, (menu.Items.Length + menu.Options.ItemsPerPage - 1) / menu.Options.ItemsPerPage);
    private static int ItemIndex(HudMenu menu, int page, int slot)
    {
        var first = page * menu.Options.ItemsPerPage;
        var visible = Math.Clamp(menu.Items.Length - first, 0, menu.Options.ItemsPerPage);
        if (menu.IsNomination && menu.Presentation.Orientation == HudMenuOrientation.Vertical && visible > 6)
        {
            var rows = (visible + 1) / 2;
            if (slot < 6) return slot < rows ? first + slot : -1;
            return slot - 6 < visible - rows ? first + rows + slot - 6 : -1;
        }
        return slot < visible ? first + slot : -1;
    }
    private static bool SameBindings(HudMenu previous, HudMenu next, int page)
    {
        var nextPage = Math.Min(page, PageCount(next) - 1);
        for (var slot = 0; slot < SlotCount; slot++)
        {
            var oldIndex = ItemIndex(previous, page, slot);
            var newIndex = ItemIndex(next, nextPage, slot);
            var oldId = oldIndex < 0 ? null : previous.Items[oldIndex].Id;
            var newId = newIndex < 0 ? null : next.Items[newIndex].Id;
            if (oldId != newId) return false;
        }
        return true;
    }
    private static float Duration(HudMenuAnimation animation) => animation switch
    {
        HudMenuAnimation.None => 0,
        HudMenuAnimation.Fast => .14f,
        HudMenuAnimation.Slow => .38f,
        _ => .24f
    };

    private void Render(Session session, Surface surface, bool show = true)
    {
        var menu = session.Menu;
        var hud = surface.Runtime;
        if (!hud.IsValid) throw new InvalidOperationException("Сущность HUD-меню удалена");
        hud.Text("Title", menu.Title); hud.Text("Subtitle", menu.Subtitle);
        hud.Text("Status", menu.Status); hud.Text("Footer", menu.Footer); hud.Text("Participation", menu.Participation);
        hud.Class("MenuRoot", "Modal", menu.Options.Modal && menu.View == HudMenuView.List);
        hud.Class("MenuRoot", "List", menu.View == HudMenuView.List);
        hud.Class("MenuRoot", "Compact", menu.View == HudMenuView.Compact);
        hud.Class("MenuRoot", "Result", menu.View == HudMenuView.Result);
        hud.Class("MenuRoot", "Nomination", menu.IsNomination);
        hud.Class("MenuRoot", "Vote", !menu.IsNomination && menu.View != HudMenuView.Result);
        hud.Class("MenuRoot", "DockLeft", menu.Presentation.DockSide == HudMenuDockSide.Left);
        hud.Class("MenuRoot", "DockRight", menu.Presentation.DockSide == HudMenuDockSide.Right);
        hud.Class("MenuRoot", "HasParticipation", !string.IsNullOrEmpty(menu.Participation));
        hud.Class("Participation", "Hidden", string.IsNullOrEmpty(menu.Participation));
        if (surface.StyleClass != menu.StyleClass)
        {
            if (surface.StyleClass.Length > 0) hud.Class("MenuRoot", surface.StyleClass, false);
            surface.StyleClass = menu.StyleClass;
        }
        if (menu.StyleClass.Length > 0) hud.Class("MenuRoot", menu.StyleClass, true);
        if (surface.VerticalGap != menu.VerticalGap)
        {
            if (surface.VerticalGap is { } previousGap) hud.Class("MenuRoot", "VerticalGap" + previousGap, false);
            if (menu.VerticalGap is { } gap) hud.Class("MenuRoot", "VerticalGap" + gap, true);
            surface.VerticalGap = menu.VerticalGap;
        }
        hud.Class("Close", "Hidden", !menu.Options.Closable || menu.View != HudMenuView.List);
        hud.Class("Back", "Hidden", !menu.ShowBack || menu.View != HudMenuView.List);
        hud.Class("Status", "Hidden", string.IsNullOrEmpty(menu.Status));
        hud.Class("Subtitle", "Hidden", string.IsNullOrEmpty(menu.Subtitle));
        hud.Class("Footer", "Hidden", string.IsNullOrEmpty(menu.Footer));
        var settingsEnabled = menu.SettingsText is not null && menu.View == HudMenuView.List;
        if (!settingsEnabled) session.SettingsOpen = false;
        hud.Class("MenuRoot", "HasSettings", settingsEnabled);
        hud.Class("MenuRoot", "HasBrand", menu.ShowBrand && menu.View == HudMenuView.List);
        hud.Class("MenuRoot", "SettingsOpen", session.SettingsOpen);
        hud.Class("MenuRoot", "Horizontal", menu.Presentation.Orientation == HudMenuOrientation.Horizontal);
        hud.Class("MenuRoot", "Vertical", menu.Presentation.Orientation == HudMenuOrientation.Vertical);
        foreach (var scale in new[] { 80, 100, 120 })
        {
            hud.Class("MenuRoot", "Scale" + scale, menu.Presentation.ScalePercent == scale);
            hud.Class("SetScale" + scale, "Selected", menu.Presentation.ScalePercent == scale);
        }
        foreach (var animation in Enum.GetValues<HudMenuAnimation>())
        {
            hud.Class("MenuRoot", "Animation" + animation, menu.Presentation.Animation == animation);
            hud.Class("SetAnimation" + animation, "Selected", menu.Presentation.Animation == animation);
        }
        hud.Class("SetHorizontal", "Selected", menu.Presentation.Orientation == HudMenuOrientation.Horizontal);
        hud.Class("SetVertical", "Selected", menu.Presentation.Orientation == HudMenuOrientation.Vertical);
        hud.Class("SetDockLeft", "Selected", menu.Presentation.DockSide == HudMenuDockSide.Left);
        hud.Class("SetDockRight", "Selected", menu.Presentation.DockSide == HudMenuDockSide.Right);
        if (menu.SettingsText is { } labels)
        {
            hud.Text("SettingsTitle", labels.Title); hud.Text("OrientationLabel", labels.Orientation);
            hud.Text("SetHorizontalText", labels.Horizontal); hud.Text("SetVerticalText", labels.Vertical);
            hud.Text("SizeLabel", labels.Size); hud.Text("SetScale80Text", labels.Scale80);
            hud.Text("SetScale100Text", labels.Scale100); hud.Text("SetScale120Text", labels.Scale120);
            hud.Text("DockSideLabel", labels.DockSide); hud.Text("SetDockLeftText", labels.Left); hud.Text("SetDockRightText", labels.Right);
            hud.Text("AnimationLabel", labels.Animation); hud.Text("SetAnimationNoneText", labels.AnimationNone);
            hud.Text("SetAnimationFastText", labels.AnimationFast); hud.Text("SetAnimationNormalText", labels.AnimationNormal);
            hud.Text("SetAnimationSlowText", labels.AnimationSlow);
            hud.Class("DockSettings", "Hidden", string.IsNullOrEmpty(labels.DockSide));
            hud.Class("AnimationSettings", "Hidden", string.IsNullOrEmpty(labels.Animation));
        }
        // Ёмкость одинакова на всех страницах; последняя страница не сжимает окно.
        var capacity = Math.Min(menu.Items.Length, menu.Options.ItemsPerPage);
        for (var count = 1; count <= SlotCount; count++) hud.Class("MenuRoot", "PageItems" + count, count == Math.Max(1, capacity));
        hud.Class("MenuRoot", "HasSecondRow", capacity > 6 && menu.View == HudMenuView.List);
        hud.Class("Row1", "Hidden", capacity <= 6 || menu.View != HudMenuView.List);
        var pages = PageCount(menu);
        hud.Text("Page", $"{session.Page + 1} / {pages}");
        hud.Class("Pagination", "Hidden", !menu.Options.ShowPagination || pages == 1 || menu.View != HudMenuView.List);
        hud.Class("PreviousPage", "Disabled", session.Page == 0);
        hud.Class("NextPage", "Disabled", session.Page + 1 >= pages);
        for (var slot = 0; slot < SlotCount; slot++)
        {
            var index = ItemIndex(menu, session.Page, slot);
            var item = index >= 0 ? menu.Items[index] : null;
            var imageClass = item?.ImagePath is { } path ? "MenuImage_custom_"
                + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(path)))[..32] : "";
            if (surface.ImageClasses[slot] != imageClass)
            {
                if (surface.ImageClasses[slot].Length > 0) hud.Class("Image" + slot, surface.ImageClasses[slot], false);
                surface.ImageClasses[slot] = imageClass;
            }
            hud.Class("Image" + slot, "HasImage", imageClass.Length > 0);
            if (imageClass.Length > 0) hud.Class("Image" + slot, imageClass, true);
            hud.Class("Item" + slot, "Hidden", item is null);
            if (item is null) continue;
            hud.Text("Name" + slot, item.Title);
            hud.Text("Description" + slot, item.Enabled ? item.Description : item.DisabledReason);
            hud.Class("Description" + slot, "Hidden", string.IsNullOrEmpty(item.Enabled ? item.Description : item.DisabledReason));
            hud.Text("Badge" + slot, item.Badge);
            hud.Class("Badge" + slot, "Hidden", string.IsNullOrEmpty(item.Badge));
            hud.Class("Item" + slot, "HasBadge", !string.IsNullOrEmpty(item.Badge));
            hud.Class("Item" + slot, "Disabled", !item.Enabled);
            hud.Class("Item" + slot, "Selected", item.Selected);
            hud.Class("Item" + slot, "HasProgress", item.Percent is not null);
            if (surface.PercentClasses[slot] != item.Percent)
            {
                if (surface.PercentClasses[slot] is { } previous) hud.Class("Bar" + slot, "Pct" + previous, false);
                if (item.Percent is { } percent) hud.Class("Bar" + slot, "Pct" + percent, true);
                surface.PercentClasses[slot] = item.Percent;
            }
        }
        if (show) hud.Show(Captures(menu));
    }

    private void OnClick(IOnCustomHudClickedEvent args)
    {
        if (!_started || _disposed || _unloading || !_sessions.TryGetValue(args.PlayerId, out var session)
            || session.Transitioning || !Captures(session.Menu) || session.Surface?.Runtime.Owns(args.CustomHudLayout) != true
            || core.PlayerManager.GetPlayer(args.PlayerId) is not { } player || !Eligible(player) || !SamePlayer(player, session)) return;
        Route(session, args.ButtonId);
    }

    private void Route(Session session, string button)
    {
        try
        {
            HudMenuAction action;
            string? itemId = null;
            if (button is "Gear" or "SettingsClose")
            {
                if (session.Menu.SettingsText is null || session.Menu.View != HudMenuView.List) return;
                if (button == "SettingsClose" && !session.SettingsOpen) return;
                session.SettingsOpen = button == "Gear" && !session.SettingsOpen;
                Render(session, session.Surface!); return;
            }
            if (button is "SetHorizontal" or "SetVertical" or "SetScale80" or "SetScale100" or "SetScale120"
                or "SetDockLeft" or "SetDockRight" or "SetAnimationNone" or "SetAnimationFast" or "SetAnimationNormal" or "SetAnimationSlow")
            {
                if (!session.SettingsOpen || session.Menu.SettingsText is not { } labels || session.Menu.View != HudMenuView.List) return;
                if (button.StartsWith("SetDock", StringComparison.Ordinal) && labels.DockSide.Length == 0
                    || button.StartsWith("SetAnimation", StringComparison.Ordinal) && labels.Animation.Length == 0) return;
                var presentation = button switch
                {
                    "SetHorizontal" => session.Menu.Presentation with { Orientation = HudMenuOrientation.Horizontal },
                    "SetVertical" => session.Menu.Presentation with { Orientation = HudMenuOrientation.Vertical },
                    "SetScale80" => session.Menu.Presentation with { ScalePercent = 80 },
                    "SetScale120" => session.Menu.Presentation with { ScalePercent = 120 },
                    "SetDockLeft" => session.Menu.Presentation with { DockSide = HudMenuDockSide.Left },
                    "SetDockRight" => session.Menu.Presentation with { DockSide = HudMenuDockSide.Right },
                    "SetAnimationNone" => session.Menu.Presentation with { Animation = HudMenuAnimation.None },
                    "SetAnimationFast" => session.Menu.Presentation with { Animation = HudMenuAnimation.Fast },
                    "SetAnimationNormal" => session.Menu.Presentation with { Animation = HudMenuAnimation.Normal },
                    "SetAnimationSlow" => session.Menu.Presentation with { Animation = HudMenuAnimation.Slow },
                    _ => session.Menu.Presentation with { ScalePercent = 100 }
                };
                var updated = session.Menu with { Presentation = presentation };
                var changedSlots = !SameBindings(session.Menu, updated, session.Page);
                session.Menu = updated;
                if (changedSlots) StagePage(session, 1);
                else Render(session, session.Surface!);
                session.Callback(new(session.Id, HudMenuAction.SettingsChanged, null) { Presentation = presentation });
                return;
            }
            if (session.SettingsOpen && button != "Close") return;
            if (button == "Close")
            {
                if (!session.Menu.Options.Closable) return;
                action = HudMenuAction.Close;
                if (session.Menu.Options.CollapseOnClose && session.Menu.View == HudMenuView.List)
                {
                    session.Menu = session.Menu with { View = HudMenuView.Compact,
                        Options = session.Menu.Options with { CaptureInput = false, Modal = false } };
                    session.SettingsOpen = false;
                    Minimize(session);
                }
                else ClosePlayer(session.PlayerId);
            }
            else if (button == "Back")
            {
                if (!session.Menu.ShowBack) return;
                action = HudMenuAction.Back;
            }
            else if (button is "NextPage" or "PreviousPage")
            {
                if (!session.Menu.Options.ShowPagination) return;
                var direction = button == "NextPage" ? 1 : -1;
                var next = session.Page + direction;
                if (next < 0 || next >= PageCount(session.Menu)) return;
                session.Page = next;
                StagePage(session, direction);
                action = direction > 0 ? HudMenuAction.NextPage : HudMenuAction.PreviousPage;
            }
            else if (button.StartsWith("Item", StringComparison.Ordinal)
                && int.TryParse(button.AsSpan(4), NumberStyles.None, CultureInfo.InvariantCulture, out var slot)
                && slot is >= 0 and < SlotCount && button == "Item" + slot)
            {
                var index = ItemIndex(session.Menu, session.Page, slot);
                if (index < 0) return;
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

    private void StagePage(Session session, int direction)
    {
        CancelTransition(session);
        // Идентификатор кнопки не содержит ревизию каталога. Новая скрытая сущность сохраняет
        // защиту от отложенных кликов, пока старая страница завершает CSS-переход без пустого кадра.
        session.Staged = new(createRuntime(session.PlayerId));
        Render(session, session.Staged, show: false);
        session.Transitioning = true;
        var suffix = direction > 0 ? "Next" : "Previous";
        session.Surface!.Runtime.Class("MenuRoot", "PageLeaving" + suffix, true);
        After(session, Duration(session.Menu.Presentation.Animation), () =>
        {
            var old = session.Surface;
            session.Surface = session.Staged!;
            session.Staged = null;
            old.Runtime.Show(false);
            old.Runtime.Class("MenuRoot", "Visible", false);
            session.Surface.Runtime.Class("MenuRoot", "PageEntering" + suffix, true);
            Render(session, session.Surface);
            old.Runtime.Dispose();
            After(session, Duration(session.Menu.Presentation.Animation), () =>
            {
                session.Surface.Runtime.Class("MenuRoot", "PageEntering" + suffix, false);
                session.Transitioning = false;
            });
        });
    }

    private void Minimize(Session session)
    {
        CancelTransition(session);
        session.SettingsOpen = false;
        session.Surface!.Runtime.Class("MenuRoot", "SettingsOpen", false);
        session.Surface.Runtime.Class("MenuRoot", "Modal", false);
        session.Surface.Runtime.Show(false);
        session.Surface.Runtime.Class("MenuRoot", "Minimizing", true);
        session.Transitioning = true;
        After(session, Duration(session.Menu.Presentation.Animation), () =>
        {
            session.Surface.Runtime.Class("MenuRoot", "Minimizing", false);
            session.Transitioning = false;
            Render(session, session.Surface);
        });
    }

    private void After(Session session, float seconds, Action action)
    {
        if (seconds <= 0) { action(); return; }
        var generation = session.TransitionGeneration;
        session.TransitionTimer = core.Scheduler.DelayBySeconds(seconds, () =>
        {
            if (_disposed || _unloading || session.TransitionGeneration != generation
                || !_sessions.TryGetValue(session.PlayerId, out var current) || !ReferenceEquals(current, session)) return;
            session.TransitionTimer = null;
            try { action(); }
            catch (Exception error) { Fail(session.PlayerId, error); }
        });
    }

    private static void CancelTransition(Session session)
    {
        session.TransitionGeneration++;
        session.TransitionTimer?.Cancel(); session.TransitionTimer = null;
        session.Staged?.Runtime.Dispose(); session.Staged = null;
        session.Transitioning = false;
        if (session.Surface is not { } surface) return;
        foreach (var name in new[] { "Minimizing", "PageLeavingNext", "PageLeavingPrevious", "PageEnteringNext", "PageEnteringPrevious" })
            surface.Runtime.Class("MenuRoot", name, false);
    }

    private void ClosePlayer(int id)
    {
        if (!_sessions.Remove(id, out var session)) return;
        try { CancelTransition(session); session.Surface?.Runtime.Dispose(); }
        catch (Exception error) { core.Logger.LogWarning(error, "[HUD Menu] Ошибка освобождения ввода игрока {PlayerId}", id); }
    }
    private void Sweep()
    {
        if (_disposed || _unloading) return;
        foreach (var (id, session) in _sessions.ToArray())
        {
            var player = core.PlayerManager.GetPlayer(id);
            if (player is null || !Eligible(player) || !SamePlayer(player, session) || session.Surface?.Runtime.IsValid != true) ClosePlayer(id);
        }
    }
    private void OnKey(IOnClientKeyStateChangedEvent args)
    {
        if (args.Key == KeyKind.Esc && args.Pressed && _sessions.TryGetValue(args.PlayerId, out var session)
            && !session.Transitioning && Captures(session.Menu)
            && core.PlayerManager.GetPlayer(args.PlayerId) is { } player && SamePlayer(player, session)) Route(session, "Close");
    }
    private void OnConnect(IOnClientConnectedEvent args)
    {
        using var timing = ConnectionDiagnostics.Begin(core.Logger, "CustomHud.Menu.client_connected", args.PlayerId);
        ClosePlayer(args.PlayerId);
    }
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

    private sealed class Surface(IHudMenuRuntime runtime)
    {
        public IHudMenuRuntime Runtime { get; } = runtime;
        public string StyleClass { get; set; } = "";
        public int? VerticalGap { get; set; }
        public string[] ImageClasses { get; } = Enumerable.Repeat("", SlotCount).ToArray();
        public int?[] PercentClasses { get; } = new int?[SlotCount];
    }

    private sealed class Session(IPlayer player, HudMenu menu, Action<HudMenuEvent> callback)
    {
        public Guid Id { get; } = Guid.NewGuid();
        public int PlayerId { get; } = player.PlayerID;
        public ulong ConnectionId { get; } = player.SessionId;
        public ulong SteamId { get; } = player.SteamID;
        public HudMenu Menu { get; set; } = menu;
        public Action<HudMenuEvent> Callback { get; } = callback;
        public Surface? Surface { get; set; }
        public Surface? Staged { get; set; }
        public int Page { get; set; }
        public bool SettingsOpen { get; set; }
        public bool Transitioning { get; set; }
        public long TransitionGeneration { get; set; }
        public CancellationTokenSource? TransitionTimer { get; set; }
    }
}
