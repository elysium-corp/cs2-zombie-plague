using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shop.Core.Application;
using Shop.Core.Data;
using Shop.Core.Menus;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;

namespace Shop.Core.Hud;

internal sealed class ShopHudMenu(
    ISwiftlyCore core,
    IOptions<ShopHudOptions> options,
    ShopHudCatalog catalog,
    ShopSnapshotCache cache,
    ShopPurchaseService purchases,
    ShopHudState state,
    ShopHudPreferences preferences,
    ShopMenu classic,
    Func<IZombiePlagueApi> zombiePlagueApi,
    ILogger<ShopHudMenu> logger) : IDisposable
{
    private readonly Dictionary<int, Session> _sessions = [];
    private readonly Dictionary<int, NativeState> _native = [];
    private readonly List<Guid> _commands = [];
    private CancellationTokenSource? _timer;
    private Guid _commandHook;
    private Guid _deathHook;
    private Guid _buyOpenHook;
    private int _buyOpenEvents;
    private int _nativeRequests;
    private int _nativeTimeouts;
    private double _traceUntil;
    private IZombiePlagueApi? _subscribedZombiePlague;
    private bool _active;
    private string? _failure;
    private double _nextRefresh;
    private static double Now => Environment.TickCount64 / 1000d;

    public void Initialize()
    {
        if (_active) return;
        _active = true;
        foreach (var command in new[] { "weapons", "shop", "магазин", "ьфпфяш" })
            _commands.Add(core.Command.RegisterCommand(command, ToggleCommand, registerRaw: true));
        _commands.Add(core.Command.RegisterCommand("shop_hud", AdminCommand, registerRaw: true, permission: "shop.admin"));
        _commandHook = core.Command.HookClientCommand(OnClientCommand);
        core.Event.OnCustomHudClicked += OnClicked;
        core.Event.OnClientKeyStateChanged += OnKey;
        core.Event.OnClientDisconnected += OnDisconnected;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnMapLoad += OnMapLoad;
        _deathHook = core.GameEvent.HookPost<EventPlayerDeath>(OnPlayerDeath);
        _buyOpenHook = core.GameEvent.HookPre<EventBuymenuOpen>(OnBuyMenuOpen);
        RebindExternalEvents();
        _timer = core.Scheduler.RepeatBySeconds(0.05f, Tick);
    }

    public void RebindExternalEvents()
    {
        if (!_active) return;
        var api = zombiePlagueApi();
        if (ReferenceEquals(_subscribedZombiePlague, api)) return;
        UnsubscribeZombiePlague();
        _subscribedZombiePlague = api;
        var players = api.Events.Players;
        players.Infected.Hook(OnPlayerInfected);
        players.Disinfected.Hook(OnPlayerDisinfected);
        players.Humanized.Hook(OnPlayerHumanized);
        players.BecameNemesis.Hook(OnPlayerBecameNemesis);
        players.BecameSurvivor.Hook(OnPlayerBecameSurvivor);
    }

    private void UnsubscribeZombiePlague()
    {
        if (_subscribedZombiePlague is not { } api) return;
        var players = api.Events.Players;
        players.Infected.Unhook(OnPlayerInfected);
        players.Disinfected.Unhook(OnPlayerDisinfected);
        players.Humanized.Unhook(OnPlayerHumanized);
        players.BecameNemesis.Unhook(OnPlayerBecameNemesis);
        players.BecameSurvivor.Unhook(OnPlayerBecameSurvivor);
        _subscribedZombiePlague = null;
    }

    // Внешний API открывает магазин идемпотентно; пользовательские команды переключают его.
    public void Open(IPlayer player)
    {
        if (!_active || !catalog.CanOpen(player)) return;
        if (state.IsOpen(player)) return;
        if (options.Value.Enabled && options.Value.ReplaceNativeBuyMenu && player.PlayerPawn?.IsBuyMenuOpen == true)
        {
            RequestNativeClose(player, true);
            return;
        }
        // Запоздалое отключение прежнего владельца слота не должно оставлять его сущность в мире.
        if (_sessions.ContainsKey(player.PlayerID)) Close(player.PlayerID);
        if (player.PlayerPawn?.IsBuyMenuOpen == true) player.ExecuteCommand("cancelselect");
        if (!options.Value.Enabled || _failure is not null)
        {
            classic.Open(player);
            return;
        }
        core.MenusAPI.CloseActiveMenu(player);
        var session = new Session(player.SessionId) { LastInteraction = Now };
        _sessions[player.PlayerID] = session;
        try
        {
            Render(player, session);
            if (_sessions.TryGetValue(player.PlayerID, out var current) && ReferenceEquals(current, session))
                state.Open(player);
        }
        catch (Exception error)
        {
            Fail(error);
            classic.Open(player);
        }
    }

    public void Close(int playerId)
    {
        state.Close(playerId);
        if (!_sessions.Remove(playerId, out var session)) return;
        try { session.Pages.Dispose(); }
        finally { session.NativeVisibility?.Dispose(); }
    }

    private void Toggle(IPlayer player)
    {
        var native = GetNativeState(player);
        if (native.Buy.Waiting)
        {
            native.Buy.Request(!native.Buy.OpenAfterClose, Now);
            return;
        }
        if (state.IsOpen(player)) Close(player.PlayerID);
        else Open(player);
    }

    private void ToggleCommand(ICommandContext context)
    {
        if (context.Sender is { IsValid: true } player) Toggle(player);
    }

    private HookResult OnClientCommand(int playerId, string commandLine)
    {
        if (!_active || !options.Value.Enabled || !options.Value.ReplaceNativeBuyMenu)
            return HookResult.Continue;
        var command = NativeBuyCommand(commandLine);
        if (command == NativeCommand.None) return HookResult.Continue;
        if (core.PlayerManager.GetPlayer(playerId) is not { IsValid: true, IsFakeClient: false } player)
            return HookResult.Continue;
        if (command == NativeCommand.Open) NativeToggle(player);
        // Выдача предметов Shop использует GiveItem, поэтому этот перехват её не затрагивает.
        return HookResult.Stop;
    }

    internal enum NativeCommand { None, Open, Purchase }

    internal static NativeCommand NativeBuyCommand(string line)
    {
        var token = line.AsSpan().TrimStart();
        if (!token.IsEmpty && token[0] == '"')
        {
            token = token[1..];
            var quote = token.IndexOf('"');
            if (quote < 0) return NativeCommand.None;
            token = token[..quote];
        }
        else
        {
            var end = token.IndexOfAny(' ', '\t', ';');
            if (end >= 0) token = token[..end];
        }
        if (token.Equals("buymenu", StringComparison.OrdinalIgnoreCase)) return NativeCommand.Open;
        return token.Equals("buy", StringComparison.OrdinalIgnoreCase)
            || token.Equals("buyrandom", StringComparison.OrdinalIgnoreCase)
            || token.Equals("autobuy", StringComparison.OrdinalIgnoreCase)
            || token.Equals("rebuy", StringComparison.OrdinalIgnoreCase)
            ? NativeCommand.Purchase : NativeCommand.None;
    }

    private void NativeToggle(IPlayer player)
    {
        var native = GetNativeState(player);
        var now = Now;
        if (now - native.LastToggle < 0.2) return;
        native.LastToggle = now;
        if (player.PlayerPawn?.IsBuyMenuOpen == true)
        {
            RequestNativeClose(player, !state.IsOpen(player));
            return;
        }
        Toggle(player);
    }

    private void RequestNativeClose(IPlayer player, bool openAfterClose)
    {
        var action = GetNativeState(player).Buy.Request(openAfterClose, Now);
        if (action == ShopNativeBuyAction.CloseNative) CloseNative(player);
    }

    private void CloseNative(IPlayer player)
    {
        Close(player.PlayerID);
        _nativeRequests++;
        // В отличие от cancelselect, buymenu адресует именно окно закупа,
        // независимо от того, какое меню получило фокус. SERVER_CAN_EXECUTE.
        player.ExecuteCommand("buymenu");
        Trace($"native close requested: player={player.PlayerID}");
    }

    private HookResult OnBuyMenuOpen(EventBuymenuOpen ev)
    {
        if (!_active || !options.Value.Enabled || !options.Value.ReplaceNativeBuyMenu) return HookResult.Continue;
        _buyOpenEvents++;
        Trace("buymenu_open received and stopped (event has no userid)");
        // Это отмена серверного уведомления, а не клиентского Panorama.
        // Владельца определяем только по его IsBuyMenuOpen, не по этому событию.
        return HookResult.Stop;
    }

    private void Trace(string message)
    {
        if (Now < _traceUntil) logger.LogInformation("[Shop HUD trace] {Message}", message);
    }

    private NativeState GetNativeState(IPlayer player)
    {
        if (!_native.TryGetValue(player.PlayerID, out var native) || native.SessionId != player.SessionId)
            _native[player.PlayerID] = native = new NativeState(player.SessionId);
        return native;
    }

    private void Tick()
    {
        if (!_active) return;
        try
        {
            var now = Now;
            var refresh = now >= _nextRefresh;
            if (refresh) _nextRefresh = now + Math.Clamp(options.Value.RefreshIntervalSeconds, 0.1f, 2f);
            foreach (var player in core.PlayerManager.GetAllPlayers())
            {
                if (!player.IsValid || player.IsFakeClient) continue;
                if (options.Value.Enabled && options.Value.ReplaceNativeBuyMenu)
                {
                    var native = GetNativeState(player);
                    var open = player.PlayerPawn?.IsBuyMenuOpen == true;
                    var action = native.Buy.Observe(open, state.IsOpen(player), now);
                    if (action == ShopNativeBuyAction.CloseNative) CloseNative(player);
                    else if (action == ShopNativeBuyAction.OpenCustom)
                    {
                        Trace($"native close confirmed: player={player.PlayerID}");
                        Open(player);
                    }
                    else if (action == ShopNativeBuyAction.TimedOut)
                    {
                        _nativeTimeouts++;
                        Trace($"native close timed out: player={player.PlayerID}");
                    }
                }
                if (!_sessions.TryGetValue(player.PlayerID, out var session)) continue;
                if (session.SessionId != player.SessionId || !catalog.CanOpen(player)
                    || session.Runtime?.IsValid != true || core.MenusAPI.GetCurrentMenu(player) is not null
                    || now - session.LastInteraction >= Math.Clamp(options.Value.IdleTimeoutSeconds, 10, 300))
                {
                    Close(player.PlayerID);
                    continue;
                }
                if (refresh) Render(player, session);
            }
        }
        catch (Exception error) { Fail(error); }
    }

    private void Render(IPlayer player, Session session)
    {
        // Покупка может синхронно вызвать смерть или смену роли. Её завершающий
        // refresh не должен заново создавать уже закрытый HUD.
        if (!_sessions.TryGetValue(player.PlayerID, out var current) || !ReferenceEquals(current, session)) return;
        if (!catalog.CanOpen(player)) { Close(player.PlayerID); return; }
        var snapshot = cache.Current;
        var view = catalog.Build(player, session.Navigation);
        if (session.View is { } previous && previous.ShopType != view.ShopType)
        {
            Close(player.PlayerID);
            return;
        }
        if (session.Pages.Bind(view, session.NavigationVersion, () => new ShopHudRuntime(core, player.PlayerID)))
            session.Choices.Clear();
        session.Snapshot = snapshot;
        session.View = view;
        var hud = session.Runtime!;
        session.LayoutColumns = Math.Max(session.LayoutColumns, view.Columns.Count);
        session.LayoutRows = Math.Max(session.LayoutRows, view.Columns.Select(x => x.Cards.Count).DefaultIfEmpty().Max());
        Choice(session, "ShopRoot", "columns", "Columns" + session.LayoutColumns);
        Choice(session, "ShopRoot", "rows", "Rows" + session.LayoutRows);
        var settings = preferences.Get(player);
        Choice(session, "ShopRoot", "scale", "Scale" + settings.ScalePercent);
        hud.Class("ShopRoot", "SettingsOpen", session.SettingsOpen);
        hud.Class("SettingsPanel", "CanEdit", settings.CanEdit);
        hud.Text("SettingsTitle", catalog.Text(player, "Shop.Hud.Settings.Size"));
        hud.Text("SettingsStatus", settings.Status == ShopHudSaveStatus.Ready ? ""
            : catalog.Text(player, "Shop.Hud.Settings." + settings.Status));
        hud.Class("SettingsStatus", "Failed", settings.Status == ShopHudSaveStatus.Failed);
        for (var index = 0; index < ShopHudPreference.Scales.Length; index++)
            hud.Class("ScaleOption" + index, "Selected", ShopHudPreference.Scales[index] == settings.ScalePercent);
        hud.Class("ShopRoot", "HasItemPages", view.Columns.Any(x => x.PageCount > 1));
        hud.Text("StoreTitle", catalog.Title(player));
        hud.Text("Balance", catalog.Balance(player));
        hud.Text("Hint", catalog.Text(player, "Shop.Hud.Hint"));
        hud.Text("Empty", catalog.Text(player, "Shop.Menu.Empty"));
        hud.Class("Empty", "Visible", view.Columns.Count == 0);
        hud.Class("CategoryPager", "Visible", view.PageCount > 1);
        hud.Text("CategoryPage", $"{view.Page + 1} / {view.PageCount}");
        hud.Class("CategoryPrev", "Available", view.Page > 0);
        hud.Class("CategoryNext", "Available", view.Page + 1 < view.PageCount);
        for (var column = 0; column < ShopHudCatalog.ColumnCount; column++)
        {
            var model = view.Columns.ElementAtOrDefault(column);
            hud.Class($"Column{column}", "Visible", model is not null);
            if (model is null) continue;
            hud.Text($"Category{column}", model.Title);
            hud.Text($"Page{column}", $"{model.Page + 1} / {model.PageCount}");
            hud.Class($"ItemPager{column}", "Visible", model.PageCount > 1);
            hud.Class($"Prev{column}", "Available", model.Page > 0);
            hud.Class($"Next{column}", "Available", model.Page + 1 < model.PageCount);
            for (var row = 0; row < ShopHudCatalog.RowCount; row++)
            {
                var slot = column * ShopHudCatalog.RowCount + row;
                var card = model.Cards.ElementAtOrDefault(row);
                var panel = $"Card{slot}";
                hud.Class(panel, "Visible", card is not null);
                if (card is null) continue;
                hud.Text($"Name{slot}", card.Name);
                hud.Text($"Price{slot}", card.Price);
                hud.Text($"Status{slot}", card.Status);
                hud.Class(panel, "Available", card.Enabled);
                Choice(session, panel, "rarity", "Rarity" + card.Rarity);
                Choice(session, $"Icon{slot}", "icon", "Icon_" + card.Icon);
            }
        }
        Choice(session, "ShopRoot", "bank", "Bank" + session.Pages.Bank);
        hud.Class("ShopRoot", "Visible", true);
        hud.Capture(true);
        if (options.Value.HideNativeHudWhileOpen)
            session.NativeVisibility ??= ShopHudNativeVisibility.Capture(player);
    }

    internal static bool SameSlots(ShopHudView? previous, ShopHudView current) => previous is not null
        && previous.ShopType == current.ShopType && previous.Page == current.Page
        && previous.Columns.Select(x => (x.Key, x.Page)).SequenceEqual(current.Columns.Select(x => (x.Key, x.Page)))
        && previous.Columns.SelectMany(x => x.Cards.Select(y => y.Offer))
            .SequenceEqual(current.Columns.SelectMany(x => x.Cards.Select(y => y.Offer)));

    private static void Choice(Session session, string panel, string group, string value)
    {
        var key = (panel, group);
        if (session.Choices.TryGetValue(key, out var old))
        {
            if (old == value) return;
            session.Runtime!.Class(panel, old, false);
        }
        session.Runtime!.Class(panel, value, true);
        session.Choices[key] = value;
    }

    private void OnClicked(IOnCustomHudClickedEvent ev)
    {
        if (!_active || !_sessions.TryGetValue(ev.PlayerId, out var session)
            || session.Runtime?.Owns(ev.CustomHudLayout) != true
            || core.PlayerManager.GetPlayer(ev.PlayerId) is not { IsValid: true } player
            || session.SessionId != player.SessionId) return;
        try
        {
            session.LastInteraction = Now;
            if (ev.ButtonId == "Close") { Close(ev.PlayerId); return; }
            if (!catalog.CanOpen(player)) { Close(ev.PlayerId); return; }
            if (ev.ButtonId == "Settings")
            {
                session.SettingsOpen = !session.SettingsOpen;
                Render(player, session);
                return;
            }
            if (session.SettingsOpen && TryIndex(ev.ButtonId, "SetScale", ShopHudPreference.Scales.Length, out var scale))
            {
                preferences.Set(player, ShopHudPreference.Scales[scale]);
                Render(player, session);
                return;
            }
            if (session.SettingsOpen || !session.Pages.TryButton(ev.ButtonId, out var button)) return;
            if (session.View is not { } view) return;
            var current = catalog.Build(player, session.Navigation);
            if (!ReferenceEquals(session.Snapshot, cache.Current) || !SameSlots(view, current))
            {
                Render(player, session);
                return;
            }
            if (button == "CategoriesPrevious" && view.Page > 0)
            {
                session.Navigation.Page--;
                session.NavigationVersion++;
            }
            else if (button == "CategoriesNext" && view.Page + 1 < view.PageCount)
            {
                session.Navigation.Page++;
                session.NavigationVersion++;
            }
            else if (TryIndex(button, "Previous", ShopHudCatalog.ColumnCount, out var previous))
            {
                if (view.Columns.ElementAtOrDefault(previous) is { Page: > 0 } column)
                {
                    session.Navigation.ItemPages[column.Key] = column.Page - 1;
                    session.NavigationVersion++;
                }
            }
            else if (TryIndex(button, "NextItems", ShopHudCatalog.ColumnCount, out var next))
            {
                if (view.Columns.ElementAtOrDefault(next) is { } column && column.Page + 1 < column.PageCount)
                {
                    session.Navigation.ItemPages[column.Key] = column.Page + 1;
                    session.NavigationVersion++;
                }
            }
            else if (TryIndex(button, "Buy", ShopHudCatalog.SlotCount, out var slot))
            {
                var card = view.Columns.ElementAtOrDefault(slot / ShopHudCatalog.RowCount)?.Cards
                    .ElementAtOrDefault(slot % ShopHudCatalog.RowCount);
                if (card is not { Enabled: true } || session.Purchasing || Now - session.LastPurchase < 0.25) return;
                session.Purchasing = true;
                session.LastPurchase = Now;
                try { purchases.TryPurchase(player, card.Offer.Id); }
                finally { session.Purchasing = false; }
            }
            else return;
            Render(player, session);
        }
        catch (Exception error) { Fail(error); }
    }

    internal static bool TryIndex(string button, string prefix, int count, out int value)
    {
        value = -1;
        return button.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(button.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out value)
            && value >= 0 && value < count && button == prefix + value.ToString(CultureInfo.InvariantCulture);
    }

    private void OnKey(IOnClientKeyStateChangedEvent ev)
    {
        if (ev.Key == KeyKind.Esc && ev.Pressed)
        {
            if (_native.TryGetValue(ev.PlayerId, out var native)) native.Buy.CancelOpen();
            Close(ev.PlayerId);
        }
    }

    private void CloseForPlayer(IPlayer player)
    {
        if (_native.TryGetValue(player.PlayerID, out var native) && native.SessionId == player.SessionId)
            native.Buy.CancelOpen();
        if (_active && _sessions.TryGetValue(player.PlayerID, out var session) && session.SessionId == player.SessionId)
            Close(player.PlayerID);
    }

    private HookResult OnPlayerDeath(EventPlayerDeath ev)
    {
        if (ev.UserIdPlayer is { } player) CloseForPlayer(player);
        return HookResult.Continue;
    }

    private void OnPlayerInfected(ref PlayerInfectedContext context) => CloseForPlayer(context.Player);
    private void OnPlayerDisinfected(ref PlayerDisinfectedContext context) => CloseForPlayer(context.Player);
    private void OnPlayerHumanized(ref PlayerHumanizedContext context) => CloseForPlayer(context.Player);
    private void OnPlayerBecameNemesis(ref PlayerBecameNemesisContext context) => CloseForPlayer(context.Player);
    private void OnPlayerBecameSurvivor(ref PlayerBecameSurvivorContext context) => CloseForPlayer(context.Player);

    private void OnDisconnected(IOnClientDisconnectedEvent ev)
    {
        Close(ev.PlayerId);
        _native.Remove(ev.PlayerId);
        preferences.Forget(ev.PlayerId);
    }
    private void OnMapUnload(IOnMapUnloadEvent ev) { CloseAll(); _native.Clear(); }
    private void OnMapLoad(IOnMapLoadEvent ev) { _failure = null; _native.Clear(); }

    private void CloseAll()
    {
        foreach (var native in _native.Values) native.Buy.CancelOpen();
        foreach (var id in _sessions.Keys.ToArray())
        {
            try { Close(id); }
            catch (Exception error) { logger.LogWarning(error, "[Shop HUD] Не удалось удалить HUD игрока {PlayerId}", id); }
        }
        state.Clear();
    }

    private void Fail(Exception error)
    {
        CloseAll();
        if (_failure is null) logger.LogError(error, "[Shop HUD] HUD остановлен, доступен shop_classic; повторить через shop_hud reload");
        _failure = error.Message;
    }

    private void AdminCommand(ICommandContext context)
    {
        if (context.Args.FirstOrDefault()?.Equals("trace", StringComparison.OrdinalIgnoreCase) == true)
            _traceUntil = Now + 30;
        if (context.Args.FirstOrDefault()?.Equals("reload", StringComparison.OrdinalIgnoreCase) == true)
        {
            CloseAll();
            _failure = null;
        }
        context.Reply($"Shop HUD: enabled={options.Value.Enabled}; replace_buy={options.Value.ReplaceNativeBuyMenu}; open={_sessions.Count}; buymenu_open_events={_buyOpenEvents}; native_close_requests={_nativeRequests}; native_close_timeouts={_nativeTimeouts}; trace={Now < _traceUntil}; error={_failure ?? "-"}");
    }

    public void Dispose()
    {
        if (!_active) return;
        _active = false;
        _timer?.Cancel();
        _timer = null;
        UnsubscribeZombiePlague();
        if (_deathHook != Guid.Empty) core.GameEvent.Unhook(_deathHook);
        _deathHook = Guid.Empty;
        if (_buyOpenHook != Guid.Empty) core.GameEvent.Unhook(_buyOpenHook);
        _buyOpenHook = Guid.Empty;
        core.Event.OnCustomHudClicked -= OnClicked;
        core.Event.OnClientKeyStateChanged -= OnKey;
        core.Event.OnClientDisconnected -= OnDisconnected;
        core.Event.OnMapUnload -= OnMapUnload;
        core.Event.OnMapLoad -= OnMapLoad;
        if (_commandHook != Guid.Empty) core.Command.UnhookClientCommand(_commandHook);
        _commandHook = Guid.Empty;
        foreach (var command in _commands) core.Command.UnregisterCommand(command);
        _commands.Clear();
        CloseAll();
        _native.Clear();
        preferences.Dispose();
    }

    private sealed class Session(ulong sessionId)
    {
        public ulong SessionId { get; } = sessionId;
        public ShopHudPages Pages { get; } = new();
        public IShopHudRuntime? Runtime => Pages.Runtime;
        public ShopHudNativeVisibility? NativeVisibility { get; set; }
        public ShopHudView? View { get; set; }
        public ShopSnapshot? Snapshot { get; set; }
        public ShopHudNavigation Navigation { get; set; } = new();
        public int NavigationVersion { get; set; }
        public Dictionary<(string, string), string> Choices { get; } = [];
        public double LastInteraction { get; set; }
        public double LastPurchase { get; set; } = double.NegativeInfinity;
        public bool Purchasing { get; set; }
        public bool SettingsOpen { get; set; }
        public int LayoutColumns { get; set; } = 1;
        public int LayoutRows { get; set; } = 1;
    }

    private sealed class NativeState(ulong sessionId)
    {
        public ulong SessionId { get; } = sessionId;
        public ShopHudNativeBuy Buy { get; } = new();
        public double LastToggle { get; set; } = double.NegativeInfinity;
    }
}
