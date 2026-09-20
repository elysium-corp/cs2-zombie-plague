using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Hud;
using CustomKnife.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Api;
using ZombiePlague.Api.Events.Contexts.Player;

namespace CustomKnife.Data.Menus;

internal sealed class KnifeMenu(
    ISwiftlyCore core,
    IKnivesRegistry registry,
    IKnifeService knives,
    IKnifeAuthorizationService authorization,
    IZombiePlagueApi zombies,
    IOptions<KnifeHudOptions> options,
    KnifeHudText text,
    Func<int, IKnifeHudRuntime> createHud,
    ILogger<KnifeMenu> logger) : IDisposable
{
    private readonly Dictionary<int, Session> _sessions = [];
    private readonly List<Guid> _commands = [];
    private CancellationTokenSource? _refresh;
    private Guid _deathHook;
    private Guid _teamHook;
    private bool _active;
    private bool _mapUnloading;
    private static double Now => Environment.TickCount64 / 1000d;

    public void RegisterCommands()
    {
        if (_active) return;
        _active = true;
        foreach (var command in new[] { "knife", "zknife", "лтшау", "ялтшау", "нож", "yj;" })
            _commands.Add(core.Command.RegisterCommand(command, OnCommand, registerRaw: true));
        core.Event.OnCustomHudClicked += OnClicked;
        core.Event.OnClientKeyStateChanged += OnKey;
        core.Event.OnClientDisconnected += OnDisconnected;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnMapLoad += OnMapLoad;
        _deathHook = core.GameEvent.HookPost<EventPlayerDeath>(OnDeath);
        _teamHook = core.GameEvent.HookPost<EventPlayerTeam>(OnTeam);
        zombies.Events.Players.Infected.Hook(OnInfected);
        zombies.Events.Players.BecameNemesis.Hook(OnNemesis);
        zombies.Events.Players.BecameSurvivor.Hook(OnSurvivor);
    }

    public void UnregisterCommands() => Dispose();

    private bool CanOpen(IPlayer player) => _active && !_mapUnloading
        && player.IsValid && !player.IsFakeClient && player.SteamID != 0
        && player.Controller.Team == Team.CT && !zombies.IsInfected(player);

    public bool Open(IPlayer player)
    {
        if (!CanOpen(player)) return false;
        if (_sessions.TryGetValue(player.PlayerID, out var existing))
        {
            if (existing.SessionId == player.SessionId && existing.Hud?.IsValid == true) return true;
            Close(player.PlayerID);
        }
        core.MenusAPI.CloseActiveMenu(player);
        var session = new Session(player.SessionId);
        _sessions[player.PlayerID] = session;
        try
        {
            if (OtherHudCaptures(player.PlayerID, session, CaptureLayouts())) { Close(player.PlayerID); return false; }
            Render(player, session);
            var interval = options.Value.RefreshIntervalSeconds;
            _refresh ??= core.Scheduler.RepeatBySeconds(float.IsFinite(interval) ? Math.Clamp(interval, 0.1f, 2f) : 0.25f, Update);
            return true;
        }
        catch (Exception error)
        {
            Fail(player.PlayerID, error);
            return false;
        }
    }

    private void OnCommand(ICommandContext context)
    {
        if (context.Sender is not { IsValid: true } player) return;
        if (_sessions.TryGetValue(player.PlayerID, out var current) && current.SessionId == player.SessionId)
        {
            Close(player.PlayerID);
            return;
        }
        if (CanOpen(player) && !Open(player))
            context.Reply(text.Get(player, "Unavailable", "Knife menu unavailable. Close other menus and check the HUD resources."));
    }

    internal void Close(int playerId)
    {
        if (!_sessions.Remove(playerId, out var session)) return;
        try { session.Hud?.Dispose(); }
        finally
        {
            if (_sessions.Count == 0)
            {
                _refresh?.Cancel();
                _refresh = null;
            }
        }
    }

    private void Update()
    {
        if (!_active || _mapUnloading) return;
        CCSCustomHudLayout[] layouts;
        try { layouts = CaptureLayouts(); }
        catch (Exception error)
        {
            logger.LogError(error, "[Knife HUD] Не удалось проверить захват ввода");
            CloseAll();
            return;
        }
        foreach (var (id, session) in _sessions.ToArray())
        {
            try
            {
                var player = core.PlayerManager.GetPlayer(id);
                if (player is null || player.SessionId != session.SessionId || !CanOpen(player)
                    || session.Hud?.IsValid != true || player.PlayerPawn?.IsBuyMenuOpen == true
                    || core.MenusAPI.GetCurrentMenu(player) is not null
                    || OtherHudCaptures(id, session, layouts)
                    || Now - session.LastInteraction >= Math.Clamp(options.Value.IdleTimeoutSeconds, 10, 600))
                {
                    Close(id);
                    continue;
                }
                Render(player, session);
            }
            catch (Exception error) { Fail(id, error); }
        }
    }

    private CCSCustomHudLayout[] CaptureLayouts() => core.EntitySystem
        .GetAllEntitiesByDesignerName<CCSCustomHudLayout>("custom_hud_layout").ToArray();

    private static bool OtherHudCaptures(int playerId, Session session, IEnumerable<CCSCustomHudLayout> layouts)
        => layouts.Any(layout => layout.IsValidEntity && session.Hud?.Owns(layout) != true
            && layout.IsInputCaptureEnabledForPlayer(playerId));

    private void Render(IPlayer player, Session session)
    {
        if (!_sessions.TryGetValue(player.PlayerID, out var active) || !ReferenceEquals(active, session)) return;
        var equipped = knives.GetKnife(player).InternalName;
        var catalog = registry.GetAll().Where(knife => knife.Enabled).ToArray();
        if (session.Selection.Bind(catalog, equipped)) ReplaceHud(session);
        var hud = session.Hud ??= createHud(player.PlayerID);
        var selection = session.Selection;
        hud.Text("Title", text.Get(player, "Title", "KNIFE SELECTOR"));
        hud.Text("Subtitle", text.Get(player, "Subtitle", "Choose your style. Dominate the game."));
        hud.Text("AvailableTitle", text.Get(player, "Available", "AVAILABLE KNIVES"));
        hud.Text("AvailableCount", $"{catalog.Count(knife => authorization.CanUse(player, knife))} / {catalog.Length}");
        hud.Text("PageLabel", text.Get(player, "Page", "PAGE {page} / {pages}", new Dictionary<string, string>
        {
            ["page"] = (selection.Page + 1).ToString(), ["pages"] = selection.PageCount.ToString()
        }));
        hud.Class("PreviousPage", "Available", selection.Page > 0);
        hud.Class("NextPage", "Available", selection.Page + 1 < selection.PageCount);
        hud.Text("BenefitsTitle", text.Get(player, "Benefits", "BENEFITS"));
        hud.Text("DescriptionTitle", text.Get(player, "Description", "DESCRIPTION"));
        hud.Text("Empty", text.Get(player, "Empty", "No knives available"));
        hud.Class("KnifeRoot", "Empty", catalog.Length == 0);
        for (var slot = 0; slot < KnifeHudSelection.PageSize; slot++)
        {
            var knife = selection.At(slot);
            hud.Class("Row" + slot, "Visible", knife is not null);
            if (knife is null) continue;
            var appearance = Appearance(knife.InternalName);
            var allowed = authorization.CanUse(player, knife);
            hud.Class("Row" + slot, "Selected", slot == selection.SelectedSlot);
            hud.Class("Row" + slot, "Equipped", equipped == knife.InternalName);
            hud.Class("Row" + slot, "Locked", !allowed);
            hud.Choice("Row" + slot, "rarity", "Rarity" + Rarity(appearance));
            hud.Choice("Image" + slot, "image", "Image_" + KnifeHudImages.Resolve(appearance.Image));
            hud.Text("Name" + slot, text.Name(player, knife));
            hud.Text("Rarity" + slot, text.Get(player, "Rarity." + Rarity(appearance), Rarity(appearance).ToString()));
            hud.Text("Action" + slot, StateText(player, equipped == knife.InternalName, allowed));
        }
        var selected = selection.Selected;
        if (selected is null)
        {
            hud.Text("FooterStatus", text.Get(player, "Empty", "No knives available"));
            hud.Text("EquipLabel", text.Get(player, "Equip", "EQUIP"));
            hud.Class("Confirm", "Available", false);
            hud.Show();
            return;
        }
        var style = Appearance(selected.InternalName);
        var canUse = authorization.CanUse(player, selected);
        var isEquipped = selected.InternalName == equipped;
        hud.Choice("Preview", "rarity", "Rarity" + Rarity(style));
        hud.Choice("PreviewImage", "image", "Image_" + KnifeHudImages.Resolve(style.Image));
        hud.Text("PreviewRarity", text.Get(player, "Rarity." + Rarity(style), Rarity(style).ToString()));
        hud.Text("PreviewName", text.Name(player, selected));
        hud.Text("PreviewSubtitle", text.Custom(player, style.SubtitleKey) ?? text.Description(player, selected));
        hud.Text("Description", text.Description(player, selected));
        var benefits = text.Benefits(player, selected, style);
        for (var index = 0; index < 4; index++)
        {
            hud.Class("BenefitRow" + index, "Visible", index < benefits.Length);
            hud.Text("Benefit" + index, benefits.ElementAtOrDefault(index) ?? "");
        }
        hud.Class("Confirm", "Available", canUse && !isEquipped);
        hud.Choice("Confirm", "slot", "Slot" + selection.SelectedSlot);
        hud.Text("EquipLabel", isEquipped || !canUse ? StateText(player, isEquipped, canUse) : text.Get(player, "Equip", "EQUIP"));
        hud.Class("Footer", "Locked", !canUse);
        hud.Class("Footer", "Equipped", isEquipped);
        hud.Text("FooterStatus", !canUse
            ? text.Get(player, "PermissionRequired", "Requires permission: {permission}", new Dictionary<string, string>
                { ["permission"] = authorization.GetRequiredPermission(selected) ?? "" })
            : isEquipped ? text.Get(player, "CurrentlyEquipped", "Currently equipped")
            : text.Get(player, "Ready", "Ready to equip"));
        hud.Show();
    }

    private KnifeHudAppearance Appearance(string id) => options.Value.Knives?.GetValueOrDefault(id) ?? new();
    private static KnifeHudRarity Rarity(KnifeHudAppearance appearance) => Enum.IsDefined(appearance.Rarity) ? appearance.Rarity : KnifeHudRarity.Common;
    private string StateText(IPlayer player, bool equipped, bool allowed) => !allowed
        ? text.Get(player, "Locked", "LOCKED")
        : equipped ? text.Get(player, "Equipped", "EQUIPPED") : text.Get(player, "Select", "SELECT");

    private static void ReplaceHud(Session session)
    {
        session.Hud?.Dispose();
        session.Hud = null;
    }

    private void OnClicked(IOnCustomHudClickedEvent ev)
    {
        if (!_active || !_sessions.TryGetValue(ev.PlayerId, out var session)
            || session.Hud?.Owns(ev.CustomHudLayout) != true
            || core.PlayerManager.GetPlayer(ev.PlayerId) is not { IsValid: true } player
            || player.SessionId != session.SessionId) return;
        try
        {
            if (ev.ButtonId == "Close") { Close(ev.PlayerId); return; }
            if (!CanOpen(player)) { Close(ev.PlayerId); return; }
            session.LastInteraction = Now;
            // До обработки кнопки сверяем исходные объекты каталога: reload с теми
            // же ID, но изменёнными правами/параметрами, инвалидирует старый клик.
            var snapshot = registry.GetAll().Where(knife => knife.Enabled).ToArray();
            if (session.Selection.Bind(snapshot, knives.GetKnife(player).InternalName))
            {
                ReplaceHud(session);
                Render(player, session);
                return;
            }
            var selection = session.Selection;
            if (ev.ButtonId is "PreviousPage" or "NextPage")
            {
                if (!selection.Move(ev.ButtonId == "NextPage" ? 1 : -1)) return;
                ReplaceHud(session);
            }
            else if (KnifeHudSelection.TrySlot(ev.ButtonId, "Preview", out var preview))
            {
                if (!selection.Preview(preview)) return;
            }
            else if (KnifeHudSelection.TrySlot(ev.ButtonId, "Equip", out var slot))
            {
                if (!selection.CanConfirm(slot) || selection.Selected is not { } knife
                    || !authorization.CanUse(player, knife)
                    || knife.InternalName == knives.GetKnife(player).InternalName
                    || Now - session.LastEquip < 0.3) return;
                session.LastEquip = Now;
                knives.SelectKnife(player, knife);
            }
            else return;
            Render(player, session);
        }
        catch (Exception error) { Fail(ev.PlayerId, error); }
    }

    private void OnKey(IOnClientKeyStateChangedEvent ev)
    {
        if (ev.Key == KeyKind.Esc && ev.Pressed) Close(ev.PlayerId);
    }
    private void OnDisconnected(IOnClientDisconnectedEvent ev) => Close(ev.PlayerId);
    private void OnMapUnload(IOnMapUnloadEvent ev) { _mapUnloading = true; CloseAll(); }
    private void OnMapLoad(IOnMapLoadEvent ev) { CloseAll(); _mapUnloading = false; }
    private HookResult OnDeath(EventPlayerDeath ev) { if (ev.UserIdPlayer is { } player) Close(player.PlayerID); return HookResult.Continue; }
    private HookResult OnTeam(EventPlayerTeam ev) { if (ev.UserIdPlayer is { } player) Close(player.PlayerID); return HookResult.Continue; }
    private void OnInfected(ref PlayerInfectedContext context) => Close(context.Player.PlayerID);
    private void OnNemesis(ref PlayerBecameNemesisContext context) => Close(context.Player.PlayerID);
    private void OnSurvivor(ref PlayerBecameSurvivorContext context) => Close(context.Player.PlayerID);

    private void Fail(int playerId, Exception error)
    {
        try { Close(playerId); }
        catch (Exception cleanupError) { logger.LogWarning(cleanupError, "[Knife HUD] Ошибка освобождения HUD игрока {PlayerId}", playerId); }
        logger.LogError(error, "[Knife HUD] Меню игрока {PlayerId} закрыто после ошибки", playerId);
    }

    private void CloseAll()
    {
        foreach (var id in _sessions.Keys.ToArray())
        {
            try { Close(id); }
            catch (Exception error) { logger.LogWarning(error, "[Knife HUD] Ошибка освобождения HUD игрока {PlayerId}", id); }
        }
    }

    public void Dispose()
    {
        if (!_active) return;
        _active = false;
        _refresh?.Cancel();
        _refresh = null;
        core.Event.OnCustomHudClicked -= OnClicked;
        core.Event.OnClientKeyStateChanged -= OnKey;
        core.Event.OnClientDisconnected -= OnDisconnected;
        core.Event.OnMapUnload -= OnMapUnload;
        core.Event.OnMapLoad -= OnMapLoad;
        zombies.Events.Players.Infected.Unhook(OnInfected);
        zombies.Events.Players.BecameNemesis.Unhook(OnNemesis);
        zombies.Events.Players.BecameSurvivor.Unhook(OnSurvivor);
        if (_deathHook != Guid.Empty) core.GameEvent.Unhook(_deathHook);
        if (_teamHook != Guid.Empty) core.GameEvent.Unhook(_teamHook);
        _deathHook = _teamHook = Guid.Empty;
        foreach (var command in _commands) core.Command.UnregisterCommand(command);
        _commands.Clear();
        CloseAll();
    }

    private sealed class Session(ulong sessionId)
    {
        public ulong SessionId { get; } = sessionId;
        public KnifeHudSelection Selection { get; } = new();
        public IKnifeHudRuntime? Hud { get; set; }
        public double LastInteraction { get; set; } = Now;
        public double LastEquip { get; set; } = double.NegativeInfinity;
    }
}
