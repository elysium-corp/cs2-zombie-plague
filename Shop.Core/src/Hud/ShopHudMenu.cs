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
    private readonly Dictionary<int, ulong> _suspendedNative = [];
    private readonly List<Guid> _commands = [];
    private CancellationTokenSource? _timer;
    private Guid _commandHook;
    private Guid _deathHook;
    private Guid _spawnHook;
    private int _nativeTriggers;
    private int _nativeCloseRequests;
    private IZombiePlagueApi? _subscribedZombiePlague;
    private bool _active;
    private bool _mapUnloading;
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
        _spawnHook = core.GameEvent.HookPost<EventPlayerSpawn>(OnPlayerSpawn);
        RebindExternalEvents();
        _timer = core.Scheduler.RepeatBySeconds(0.05f, Update);
        Update();
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
        if (!_active || _mapUnloading || !catalog.CanOpen(player)) return;
        if (state.IsOpen(player)) return;
        if (!options.Value.Enabled || _failure is not null)
        {
            classic.Open(player);
            return;
        }
        core.MenusAPI.CloseActiveMenu(player);
        _suspendedNative.Remove(player.PlayerID);
        try
        {
            var session = Prepare(player);
            session.Presentation.Open(player.PlayerPawn?.IsBuyMenuOpen == true);
            session.Closing = false;
            session.LastInteraction = Now;
            Render(player, session);
            if (!_sessions.TryGetValue(player.PlayerID, out var current) || !ReferenceEquals(current, session)) return;
            DismissNative(player, session);
        }
        catch (Exception error)
        {
            Fail(error);
            classic.Open(player);
        }
    }

    public void Close(int playerId, bool closeNative = true)
    {
        state.Close(playerId);
        if (!_sessions.Remove(playerId, out var session)) return;
        try
        {
            if (closeNative && !_mapUnloading && core.PlayerManager.GetPlayer(playerId) is { IsValid: true } player && player.SessionId == session.SessionId
                && player.PlayerPawn?.IsBuyMenuOpen == true)
            {
                _suspendedNative[playerId] = player.SessionId;
                DismissNative(player, session);
            }
        }
        finally
        {
            try { session.Pages.Dispose(); }
            finally { session.NativeVisibility?.Dispose(); }
        }
    }

    private void CloseWithAnimation(int playerId)
    {
        if (!_sessions.TryGetValue(playerId, out var session) || session.Closing) return;
        if (core.PlayerManager.GetPlayer(playerId) is not { IsValid: true } player || player.SessionId != session.SessionId) return;
        if (!session.Presentation.Visible)
        {
            if (player.PlayerPawn?.IsBuyMenuOpen == true) Close(playerId);
            return;
        }
        DismissNative(player, session);
        var appearance = session.View is { } view && cache.Current.Storefronts.TryGetValue(view.ShopType, out var store)
            ? store.Appearance : ShopHudAppearance.Default;
        if (appearance.CloseAnimation == "none" || session.Runtime?.IsValid != true) { Close(playerId); return; }
        session.Closing = true;
        session.CloseAt = Now + appearance.Duration;
        session.Runtime.Class("ShopRoot", "Closing", true);
    }

    private void Toggle(IPlayer player)
    {
        if (state.IsOpen(player)) CloseWithAnimation(player.PlayerID);
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
        if (core.PlayerManager.GetPlayer(playerId) is not { IsValid: true, IsFakeClient: false })
            return HookResult.Continue;
        // Выдача предметов Shop использует GiveItem, поэтому этот перехват её не затрагивает.
        return HookResult.Stop;
    }

    internal enum NativeCommand { None, Purchase = 2 }

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
        return token.Equals("buy", StringComparison.OrdinalIgnoreCase)
            || token.Equals("buyrandom", StringComparison.OrdinalIgnoreCase)
            || token.Equals("autobuy", StringComparison.OrdinalIgnoreCase)
            || token.Equals("rebuy", StringComparison.OrdinalIgnoreCase)
            ? NativeCommand.Purchase : NativeCommand.None;
    }

    private Session Prepare(IPlayer player)
    {
        if (_sessions.TryGetValue(player.PlayerID, out var current))
        {
            if (current.SessionId == player.SessionId) return current;
            Close(player.PlayerID);
        }
        var session = new Session(player.SessionId, options.Value.ReplaceNativeBuyMenu);
        _sessions[player.PlayerID] = session;
        Render(player, session);
        state.Track(player, () => session.Runtime?.IsValid == true
            && session.Presentation.IsOpen(player.PlayerPawn?.IsBuyMenuOpen == true));
        return session;
    }

    private void DismissNative(IPlayer player, Session session)
    {
        if (session.Presentation.RequestNativeClose(player.PlayerPawn?.IsBuyMenuOpen == true))
        {
            _nativeCloseRequests++;
            player.ExecuteCommand("buymenu");
        }
    }

    private void ProcessNativeTrigger(IPlayer player, Session session)
    {
        var nativeOpen = player.PlayerPawn?.IsBuyMenuOpen == true;
        if (!session.Presentation.ObserveNativeTrigger(nativeOpen)) return;
        _nativeTriggers++;
        if (session.Presentation.Visible)
        {
            CloseWithAnimation(player.PlayerID);
            return;
        }
        session.Presentation.Open(nativeOpen);
        session.LastInteraction = Now;
        // Сначала закрепляем CSS Visible и ввод. Закрытие CS2 больше не является
        // условием показа Shop и не снимает его видимость.
        Render(player, session);
        if (!_sessions.TryGetValue(player.PlayerID, out var current) || !ReferenceEquals(current, session)) return;
        DismissNative(player, session);
    }

    private void UpdateInput(IPlayer player, Session session)
    {
        if (!_sessions.TryGetValue(player.PlayerID, out var current) || !ReferenceEquals(current, session)) return;
        var open = session.Presentation.Visible;
        session.Runtime?.Capture(open);
        if (open && options.Value.HideNativeHudWhileOpen)
            session.NativeVisibility ??= ShopHudNativeVisibility.Capture(player);
        else
        {
            session.NativeVisibility?.Dispose();
            session.NativeVisibility = null;
        }
    }

    // Служебное обновление ввода, данных и анимаций. Первый кадр оверлея
    // показывает HUD_BUYMENU_VISIBLE на клиенте, независимо от этого таймера.
    private void Update()
    {
        if (!_active || _mapUnloading || !options.Value.Enabled || _failure is not null) return;
        try
        {
            var now = Now;
            var refresh = now >= _nextRefresh;
            if (refresh) _nextRefresh = now + Math.Clamp(options.Value.RefreshIntervalSeconds, 0.1f, 2f);
            foreach (var player in core.PlayerManager.GetAllPlayers())
            {
                if (!player.IsValid || player.IsFakeClient) continue;
                if (_suspendedNative.TryGetValue(player.PlayerID, out var suspended))
                {
                    if (suspended == player.SessionId && player.PlayerPawn?.IsBuyMenuOpen == true) continue;
                    _suspendedNative.Remove(player.PlayerID);
                }
                if (!catalog.CanOpen(player) || core.MenusAPI.GetCurrentMenu(player) is not null)
                {
                    Close(player.PlayerID);
                    continue;
                }
                _sessions.TryGetValue(player.PlayerID, out var session);
                if (session is not null && session.SessionId != player.SessionId)
                {
                    Close(player.PlayerID);
                    session = null;
                }
                if (session is null)
                {
                    if (!options.Value.ReplaceNativeBuyMenu) continue;
                    session = Prepare(player);
                }
                if (session.Runtime?.IsValid != true)
                {
                    Close(player.PlayerID);
                    continue;
                }
                ProcessNativeTrigger(player, session);
                if (!_sessions.TryGetValue(player.PlayerID, out var current) || !ReferenceEquals(current, session)) continue;
                if (session.Closing)
                {
                    if (now >= session.CloseAt) Close(player.PlayerID);
                    continue;
                }
                if (session.Presentation.Visible
                    && now - session.LastInteraction >= Math.Clamp(options.Value.IdleTimeoutSeconds, 10, 300))
                {
                    CloseWithAnimation(player.PlayerID);
                    continue;
                }
                if (session.Transition.Advance(now) || refresh) Render(player, session);
                UpdateInput(player, session);
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
        var appearance = snapshot.Storefronts[view.ShopType].Appearance.WithFrame(snapshot.Frame);
        foreach (var (group, name) in appearance.Classes()) Choice(session, "ShopRoot", group, name);
        Choice(session, "ShopRoot", "storefront", "Storefront_" + view.ShopType.ToString().ToLowerInvariant());
        Choice(session, "ShopRoot", "columns", "Columns" + appearance.Columns);
        Choice(session, "ShopRoot", "rows", "Rows" + appearance.Rows);
        var settings = preferences.Get(player, snapshot.Frame.DefaultScale);
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
        hud.Class("ShopRoot", "MotionBusy", session.Transition.Busy);
        Choice(session, "Columns", "motion", session.Transition.ClassFor("Columns"));
        for (var index = 0; index < ShopHudCatalog.ColumnCount; index++)
            Choice(session, $"Cards{index}", "motion", session.Transition.ClassFor($"Cards{index}"));
        var selected = view.Columns.SelectMany(x => x.Cards).FirstOrDefault(x => x.Offer.Id == session.SelectedOfferId);
        hud.Class("ShopRoot", "HasSelection", appearance.ClickBehavior == "confirm" && selected is not null);
        hud.Text("SelectionName", selected is null ? "" : selected.Name + " · " + selected.Price);
        hud.Text("ConfirmLabel", catalog.Text(player, "Shop.Hud.Confirm"));
        hud.Class("Confirm", "Available", selected is { Enabled: true });
        Choice(session, "Confirm", "rarity", "Rarity" + (selected?.Rarity.ToString() ?? "Common"));
        var selectedSlot = view.Columns.SelectMany((column, index) => column.Cards.Select((card, row) =>
            (card.Offer.Id, Slot: index * ShopHudCatalog.RowCount + row))).FirstOrDefault(x => x.Id == session.SelectedOfferId, (Id: 0L, Slot: -1)).Slot;
        Choice(session, "Confirm", "slot", "ConfirmSlot" + selectedSlot);
        Choice(session, "ShopRoot", "selectionPulse", "Pulse" + session.SelectionPulse);
        hud.Text("Empty", catalog.Text(player, "Shop.Menu.Empty"));
        hud.Class("Empty", "Visible", view.Columns.Count == 0);
        hud.Class("CategoryPager", "Visible", view.PageCount > 1);
        hud.Text("CategoryPage", $"{view.Page + 1} / {view.PageCount}");
        hud.Class("CategoryPrev", "Available", (appearance.WrapPages && view.PageCount > 1) || view.Page > 0);
        hud.Class("CategoryNext", "Available", (appearance.WrapPages && view.PageCount > 1) || view.Page + 1 < view.PageCount);
        for (var column = 0; column < ShopHudCatalog.ColumnCount; column++)
        {
            var model = view.Columns.ElementAtOrDefault(column);
            hud.Class($"Column{column}", "Visible", model is not null);
            if (model is null) continue;
            hud.Text($"Category{column}", model.Title);
            hud.Text($"Page{column}", $"{model.Page + 1} / {model.PageCount}");
            hud.Class($"ItemPager{column}", "Visible", model.PageCount > 1);
            hud.Class($"Prev{column}", "Available", (appearance.WrapPages && model.PageCount > 1) || model.Page > 0);
            hud.Class($"Next{column}", "Available", (appearance.WrapPages && model.PageCount > 1) || model.Page + 1 < model.PageCount);
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
                hud.Class($"Cooldown{slot}", "Visible", card.CooldownSeconds > 0);
                hud.Text($"Countdown{slot}", ShopHudCountdown.Format(card.CooldownSeconds, appearance.TimerFormat, key => catalog.Text(player, key)));
                Choice(session, $"Cooldown{slot}", "progress", "Progress" + ShopHudCountdown.Progress(card.CooldownSeconds, card.Offer.CooldownSeconds));
                hud.Class(panel, "Available", card.Enabled);
                hud.Class(panel, "Selected", card.Offer.Id == session.HighlightOfferId);
                Choice(session, panel, "rarity", "Rarity" + card.Rarity);
                Choice(session, $"Icon{slot}", "icon", "Icon_" + card.Icon);
            }
        }
        Choice(session, "ShopRoot", "bank", "Bank" + session.Pages.Bank);
        hud.Class("ShopRoot", "NativeBuyTrigger", session.Presentation.NativeTriggerEnabled);
        hud.Class("ShopRoot", "Visible", session.Presentation.Visible);
        hud.Class("ShopRoot", "Closing", session.Closing);
        UpdateInput(player, session);
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
            || session.SessionId != player.SessionId || session.Closing || !session.Presentation.Visible) return;
        try
        {
            session.LastInteraction = Now;
            if (ev.ButtonId == "Close") { CloseWithAnimation(ev.PlayerId); return; }
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
            if (session.SettingsOpen || session.Transition.Busy || !session.Pages.TryButton(ev.ButtonId, out var button)) return;
            if (session.View is not { } view) return;
            var current = catalog.Build(player, session.Navigation);
            if (!ReferenceEquals(session.Snapshot, cache.Current) || !SameSlots(view, current))
            {
                Render(player, session);
                return;
            }
            var appearance = cache.Current.Storefronts[view.ShopType].Appearance;
            var wrap = appearance.WrapPages;
            if (button == "CategoriesPrevious" && (wrap || view.Page > 0))
            {
                Navigate(session, "Columns", -1, appearance, () =>
                    session.Navigation.Page = ShopHudAppearance.MovePage(view.Page, view.PageCount, -1, wrap));
            }
            else if (button == "CategoriesNext" && (wrap || view.Page + 1 < view.PageCount))
            {
                Navigate(session, "Columns", 1, appearance, () =>
                    session.Navigation.Page = ShopHudAppearance.MovePage(view.Page, view.PageCount, 1, wrap));
            }
            else if (TryIndex(button, "Previous", ShopHudCatalog.ColumnCount, out var previous))
            {
                if (view.Columns.ElementAtOrDefault(previous) is { } column && (wrap || column.Page > 0))
                {
                    Navigate(session, $"Cards{previous}", -1, appearance, () =>
                        session.Navigation.ItemPages[column.Key] = ShopHudAppearance.MovePage(column.Page, column.PageCount, -1, wrap));
                }
            }
            else if (TryIndex(button, "NextItems", ShopHudCatalog.ColumnCount, out var next))
            {
                if (view.Columns.ElementAtOrDefault(next) is { } column && (wrap || column.Page + 1 < column.PageCount))
                {
                    Navigate(session, $"Cards{next}", 1, appearance, () =>
                        session.Navigation.ItemPages[column.Key] = ShopHudAppearance.MovePage(column.Page, column.PageCount, 1, wrap));
                }
            }
            else if (TryIndex(button, "Confirm", ShopHudCatalog.SlotCount, out var confirmationSlot))
            {
                var selected = current.Columns.ElementAtOrDefault(confirmationSlot / ShopHudCatalog.RowCount)?.Cards
                    .ElementAtOrDefault(confirmationSlot % ShopHudCatalog.RowCount);
                if (appearance.ClickBehavior != "confirm" || selected is not { Enabled: true } || selected.Offer.Id != session.SelectedOfferId) return;
                if (PurchaseCard(player, session, selected)) session.SelectedOfferId = null;
            }
            else if (TryIndex(button, "Buy", ShopHudCatalog.SlotCount, out var slot))
            {
                var card = current.Columns.ElementAtOrDefault(slot / ShopHudCatalog.RowCount)?.Cards
                    .ElementAtOrDefault(slot % ShopHudCatalog.RowCount);
                if (card is not { Enabled: true }) return;
                session.HighlightOfferId = card.Offer.Id;
                session.SelectionPulse = session.SelectionPulse == "A" ? "B" : "A";
                if (appearance.ClickBehavior == "confirm") session.SelectedOfferId = card.Offer.Id;
                else if (PurchaseCard(player, session, card) && appearance.ClickBehavior == "buy_close")
                {
                    Render(player, session);
                    if (_sessions.TryGetValue(player.PlayerID, out var active) && ReferenceEquals(active, session))
                        CloseWithAnimation(player.PlayerID);
                    return;
                }
            }
            else return;
            Render(player, session);
        }
        catch (Exception error) { Fail(error); }
    }

    private static void Navigate(Session session, string panel, int direction, ShopHudAppearance appearance, Action update)
    {
        session.Transition.Begin(panel, direction, Now, appearance, () =>
        {
            update();
            session.NavigationVersion++;
            session.SelectedOfferId = null;
            session.HighlightOfferId = null;
        });
    }

    private bool PurchaseCard(IPlayer player, Session session, ShopHudCard card)
    {
        if (session.Purchasing || Now - session.LastPurchase < 0.25) return false;
        session.Purchasing = true;
        session.LastPurchase = Now;
        try { return purchases.TryPurchase(player, card.Offer.Id); }
        finally { session.Purchasing = false; }
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
        if (!_active || ev.Key != KeyKind.Esc || !ev.Pressed || !_sessions.TryGetValue(ev.PlayerId, out var session)) return;
        session.Presentation.CancelNativeTrigger();
        if (session.Presentation.Visible) CloseWithAnimation(ev.PlayerId);
        else Close(ev.PlayerId);
    }

    private void CloseForPlayer(IPlayer player)
    {
        if (_active && _sessions.TryGetValue(player.PlayerID, out var session) && session.SessionId == player.SessionId)
            Close(player.PlayerID);
    }

    private HookResult OnPlayerSpawn(EventPlayerSpawn ev)
    {
        // Состояние стороны может выставляться другими плагинами после spawn;
        // служебное обновление также подготавливает HUD после завершения их событий.
        Update();
        return HookResult.Continue;
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
        Close(ev.PlayerId, closeNative: false);
        _suspendedNative.Remove(ev.PlayerId);
        preferences.Forget(ev.PlayerId);
    }
    private void OnMapUnload(IOnMapUnloadEvent ev) { _mapUnloading = true; CloseAll(); _suspendedNative.Clear(); }
    private void OnMapLoad(IOnMapLoadEvent ev) { _failure = null; _mapUnloading = false; _suspendedNative.Clear(); }

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
        context.Reply($"Shop HUD: enabled={options.Value.Enabled}; replace_buy={options.Value.ReplaceNativeBuyMenu}; mode=css_trigger; prepared={_sessions.Count}; open={_sessions.Values.Count(x => x.Presentation.Visible)}; native_triggers={_nativeTriggers}; native_close_requests={_nativeCloseRequests}; error={_failure ?? "-"}");
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
        if (_spawnHook != Guid.Empty) core.GameEvent.Unhook(_spawnHook);
        _spawnHook = Guid.Empty;
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
        _suspendedNative.Clear();
        preferences.Dispose();
    }

    private sealed class Session(ulong sessionId, bool followNative)
    {
        public ulong SessionId { get; } = sessionId;
        public ShopHudPresentation Presentation { get; } = new(followNative);
        public ShopHudPages Pages { get; } = new();
        public ShopHudTransition Transition { get; } = new();
        public IShopHudRuntime? Runtime => Pages.Runtime;
        public ShopHudNativeVisibility? NativeVisibility { get; set; }
        public ShopHudView? View { get; set; }
        public ShopSnapshot? Snapshot { get; set; }
        public ShopHudNavigation Navigation { get; set; } = new();
        public int NavigationVersion { get; set; }
        public Dictionary<(string, string), string> Choices { get; } = [];
        public double LastInteraction { get; set; }
        public double LastPurchase { get; set; } = double.NegativeInfinity;
        public long? SelectedOfferId { get; set; }
        public long? HighlightOfferId { get; set; }
        public string SelectionPulse { get; set; } = "A";
        public bool Purchasing { get; set; }
        public bool SettingsOpen { get; set; }
        public bool Closing { get; set; }
        public double CloseAt { get; set; }
    }
}
