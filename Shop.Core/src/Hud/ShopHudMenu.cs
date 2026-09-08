using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Shop.Core.Application;
using Shop.Core.Data;
using Shop.Core.Menus;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Hud;

internal sealed class ShopHudMenu(
    ISwiftlyCore core,
    IOptions<ShopHudOptions> options,
    ShopHudCatalog catalog,
    ShopSnapshotCache cache,
    ShopPurchaseService purchases,
    ShopHudState state,
    ShopMenu classic,
    ILogger<ShopHudMenu> logger) : IDisposable
{
    private readonly Dictionary<int, Session> _sessions = [];
    private readonly Dictionary<int, NativeState> _native = [];
    private readonly List<Guid> _commands = [];
    private CancellationTokenSource? _timer;
    private Guid _commandHook;
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
        _timer = core.Scheduler.RepeatBySeconds(0.05f, Tick);
    }

    // Внешний API открывает магазин идемпотентно; пользовательские команды переключают его.
    public void Open(IPlayer player)
    {
        if (!_active || !catalog.CanOpen(player)) return;
        if (state.IsOpen(player)) return;
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
        session.Runtime?.Dispose();
    }

    private void Toggle(IPlayer player)
    {
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
        // buymenu — клиентская команда. cancelselect разрешена SERVER_CAN_EXECUTE.
        // Не изменяем IsBuyMenuOpen сами: ждём подтверждённое состояние клиента.
        if (state.IsOpen(player))
        {
            if (player.PlayerPawn?.IsBuyMenuOpen == true) player.ExecuteCommand("cancelselect");
            Close(player.PlayerID);
        }
        else Open(player);
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
                    var rising = open && !native.WasOpen;
                    native.WasOpen = open;
                    if (rising) NativeToggle(player);
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
        var snapshot = cache.Current;
        var view = catalog.Build(player, session.Navigation);
        if (session.View is { } previous && previous.ShopType != view.ShopType)
        {
            session.Navigation = new();
            view = catalog.Build(player, session.Navigation);
        }
        // Новый пул кнопок получает новую сущность. Запоздалый клик со старой страницы
        // не должен приобрести другой предмет, занявший тот же визуальный слот.
        if (session.Runtime is null || !SameSlots(session.View, view) || !ReferenceEquals(session.Snapshot, snapshot))
        {
            session.Runtime?.Dispose();
            session.Runtime = null;
            session.Choices.Clear();
            session.Runtime = new ShopHudRuntime(core, player.PlayerID);
        }
        session.Snapshot = snapshot;
        session.View = view;
        var hud = session.Runtime;
        hud.Text("StoreTitle", catalog.Title(player));
        hud.Text("Balance", catalog.Balance(player));
        hud.Text("Hint", catalog.Text(player, "Shop.Hud.Hint"));
        hud.Text("Empty", catalog.Text(player, "Shop.Menu.Empty"));
        hud.Class("Empty", "Visible", view.Columns.Count == 0);
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
        hud.Class("ShopRoot", "Visible", true);
        hud.Capture(true);
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
            if (session.View is not { } view) return;
            var current = catalog.Build(player, session.Navigation);
            if (!ReferenceEquals(session.Snapshot, cache.Current) || !SameSlots(view, current))
            {
                Render(player, session);
                return;
            }
            if (ev.ButtonId == "CategoriesPrevious" && view.Page > 0) session.Navigation.Page--;
            else if (ev.ButtonId == "CategoriesNext" && view.Page + 1 < view.PageCount) session.Navigation.Page++;
            else if (TryIndex(ev.ButtonId, "Previous", ShopHudCatalog.ColumnCount, out var previous))
            {
                if (view.Columns.ElementAtOrDefault(previous) is { Page: > 0 } column)
                    session.Navigation.ItemPages[column.Key] = column.Page - 1;
            }
            else if (TryIndex(ev.ButtonId, "NextItems", ShopHudCatalog.ColumnCount, out var next))
            {
                if (view.Columns.ElementAtOrDefault(next) is { } column && column.Page + 1 < column.PageCount)
                    session.Navigation.ItemPages[column.Key] = column.Page + 1;
            }
            else if (TryIndex(ev.ButtonId, "Buy", ShopHudCatalog.SlotCount, out var slot))
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
        if (ev.Key == KeyKind.Esc && ev.Pressed) Close(ev.PlayerId);
    }

    private void OnDisconnected(IOnClientDisconnectedEvent ev) { Close(ev.PlayerId); _native.Remove(ev.PlayerId); }
    private void OnMapUnload(IOnMapUnloadEvent ev) { CloseAll(); _native.Clear(); }
    private void OnMapLoad(IOnMapLoadEvent ev) { _failure = null; _native.Clear(); }

    private void CloseAll()
    {
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
        if (context.Args.FirstOrDefault()?.Equals("reload", StringComparison.OrdinalIgnoreCase) == true)
        {
            CloseAll();
            _failure = null;
        }
        context.Reply($"Shop HUD: enabled={options.Value.Enabled}; replace_buy={options.Value.ReplaceNativeBuyMenu}; open={_sessions.Count}; error={_failure ?? "-"}");
    }

    public void Dispose()
    {
        if (!_active) return;
        _active = false;
        _timer?.Cancel();
        _timer = null;
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
    }

    private sealed class Session(ulong sessionId)
    {
        public ulong SessionId { get; } = sessionId;
        public IShopHudRuntime? Runtime { get; set; }
        public ShopHudView? View { get; set; }
        public ShopSnapshot? Snapshot { get; set; }
        public ShopHudNavigation Navigation { get; set; } = new();
        public Dictionary<(string, string), string> Choices { get; } = [];
        public double LastInteraction { get; set; }
        public double LastPurchase { get; set; } = double.NegativeInfinity;
        public bool Purchasing { get; set; }
    }

    private sealed class NativeState(ulong sessionId)
    {
        public ulong SessionId { get; } = sessionId;
        public bool WasOpen { get; set; }
        public double LastToggle { get; set; } = double.NegativeInfinity;
    }
}
