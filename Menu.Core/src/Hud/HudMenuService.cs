using Menu.Api.Hud;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.NetMessages;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace Menu.Core.Hud;

internal sealed class HudMenuService(ISwiftlyCore core) : IHudMenuApi
{
    private readonly Dictionary<string, MenuRuntime> _menus = new(StringComparer.Ordinal);
    private readonly Dictionary<int, MenuSession> _sessions = [];

    private Guid? _clickHook;
    private bool _initialized;

    public void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        _clickHook = core.NetMessage.HookClientMessage<CCSUsrMsg_CustomHudClicked>(OnCustomHudClicked);
        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnClientDisconnected += OnClientDisconnected;
        _initialized = true;
    }

    public void Shutdown()
    {
        if (!_initialized)
        {
            return;
        }

        _initialized = false;

        if (_clickHook is { } clickHook)
        {
            core.NetMessage.Unhook(clickHook);
            _clickHook = null;
        }

        core.Event.OnMapLoad -= OnMapLoad;
        core.Event.OnMapUnload -= OnMapUnload;
        core.Event.OnClientDisconnected -= OnClientDisconnected;

        foreach (var playerId in _sessions.Keys.ToArray())
        {
            Close(playerId);
        }

        foreach (var runtime in _menus.Values)
        {
            Despawn(runtime);
        }

        _menus.Clear();
    }

    public IDisposable Register(HudMenuDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        var runtime = new MenuRuntime(DefinitionSnapshot.From(definition));
        if (!_menus.TryAdd(runtime.Definition.Id, runtime))
        {
            throw new InvalidOperationException(
                $"HUD menu '{runtime.Definition.Id}' is already registered."
            );
        }

        return new Registration(() => Unregister(runtime));
    }

    public void Open(IPlayer player, string menuId, HudMenuView view)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(menuId);
        ArgumentNullException.ThrowIfNull(view);

        if (!player.IsValid || player.IsFakeClient)
        {
            return;
        }

        var runtime = GetRuntime(menuId);
        var entity = EnsureEntity(runtime);
        var playerId = player.PlayerID;

        Close(playerId);
        core.MenusAPI.CloseActiveMenu(player);

        var session = new MenuSession(runtime, ViewSnapshot.From(view));
        _sessions[playerId] = session;

        try
        {
            ApplyView(entity, playerId, previous: null, session.View);
            SetClass(entity, playerId, runtime.Definition.RootPanelId, runtime.Definition.OpenClassName, enabled: true);
            entity.SetInputCaptureEnabledForPlayer(playerId, enabled: true);
        }
        catch
        {
            Close(playerId);
            throw;
        }
    }

    public bool Update(IPlayer player, string menuId, HudMenuView view)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(menuId);
        ArgumentNullException.ThrowIfNull(view);

        if (!player.IsValid ||
            !_sessions.TryGetValue(player.PlayerID, out var session) ||
            !string.Equals(session.Runtime.Definition.Id, menuId, StringComparison.Ordinal) ||
            session.Runtime.Entity is not { IsValidEntity: true } entity)
        {
            return false;
        }

        var next = ViewSnapshot.From(view);
        ApplyView(entity, player.PlayerID, session.View, next);
        session.View = next;
        return true;
    }

    public void Close(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);
        Close(player.PlayerID);
    }

    public bool IsOpen(IPlayer player, string menuId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentException.ThrowIfNullOrWhiteSpace(menuId);

        return _sessions.TryGetValue(player.PlayerID, out var session) &&
               string.Equals(session.Runtime.Definition.Id, menuId, StringComparison.Ordinal);
    }

    private MenuRuntime GetRuntime(string menuId)
    {
        if (!_menus.TryGetValue(menuId, out var runtime))
        {
            throw new KeyNotFoundException($"HUD menu '{menuId}' is not registered.");
        }

        return runtime;
    }

    private CCSCustomHudLayout EnsureEntity(MenuRuntime runtime)
    {
        if (runtime.Entity is { IsValidEntity: true } current)
        {
            return current;
        }

        var entity = core.EntitySystem.CreateEntity<CCSCustomHudLayout>();
        entity.StrLayout = runtime.Definition.LayoutPath;
        entity.StrLayoutUpdated();
        entity.DispatchSpawn();

        runtime.Entity = entity;

        core.Logger.LogInformation(
            "[Menu] Создан custom_hud_layout #{EntityIndex} для меню {MenuId}: {LayoutPath}",
            entity.Index,
            runtime.Definition.Id,
            runtime.Definition.LayoutPath
        );

        return entity;
    }

    private void Close(int playerId)
    {
        if (!_sessions.Remove(playerId, out var session))
        {
            return;
        }

        if (session.Runtime.Entity is not { IsValidEntity: true } entity)
        {
            return;
        }

        try
        {
            entity.SetInputCaptureEnabledForPlayer(playerId, enabled: false);
            SetClass(
                entity,
                playerId,
                session.Runtime.Definition.RootPanelId,
                session.Runtime.Definition.OpenClassName,
                enabled: false
            );
            ClearView(entity, playerId, session.View);
        }
        catch (Exception exception)
        {
            core.Logger.LogWarning(
                exception,
                "[Menu] Не удалось полностью закрыть HUD-меню {MenuId} для игрока {PlayerId}",
                session.Runtime.Definition.Id,
                playerId
            );
        }
    }

    private static void ApplyView(
        CCSCustomHudLayout entity,
        int playerId,
        ViewSnapshot? previous,
        ViewSnapshot next)
    {
        if (previous is not null)
        {
            foreach (var oldVariable in previous.Variables.Values)
            {
                if (!next.Variables.ContainsKey((oldVariable.PanelId, oldVariable.VariableName)))
                {
                    entity.RemoveDialogVariableStringForPlayer(
                        playerId,
                        oldVariable.PanelId,
                        oldVariable.VariableName
                    );
                }
            }

            foreach (var oldClass in previous.Classes.Values)
            {
                if (!next.Classes.ContainsKey((oldClass.PanelId, oldClass.ClassName)))
                {
                    SetClass(entity, playerId, oldClass.PanelId, oldClass.ClassName, enabled: false);
                }
            }
        }

        foreach (var variable in next.Variables.Values)
        {
            entity.SetDialogVariableStringForPlayer(
                playerId,
                variable.PanelId,
                variable.VariableName,
                variable.Value
            );
        }

        foreach (var panelClass in next.Classes.Values)
        {
            SetClass(entity, playerId, panelClass.PanelId, panelClass.ClassName, panelClass.Enabled);
        }
    }

    private static void ClearView(CCSCustomHudLayout entity, int playerId, ViewSnapshot view)
    {
        foreach (var variable in view.Variables.Values)
        {
            entity.RemoveDialogVariableStringForPlayer(playerId, variable.PanelId, variable.VariableName);
        }

        foreach (var panelClass in view.Classes.Values)
        {
            SetClass(entity, playerId, panelClass.PanelId, panelClass.ClassName, enabled: false);
        }
    }

    private static void SetClass(
        CCSCustomHudLayout entity,
        int playerId,
        string panelId,
        string className,
        bool enabled)
    {
        entity.SetHasClassForPlayer(
            playerId,
            panelId,
            className,
            enabled
                ? EHudPanelClassStatus_t.k_eHudPanelClassStatus_HasClass
                : EHudPanelClassStatus_t.k_eHudPanelClassStatus_DoesNotHaveClass
        );
    }

    private HookResult OnCustomHudClicked(CCSUsrMsg_CustomHudClicked message, int playerId)
    {
        var layoutHandle = message.CustomHudLayout;
        var buttonId = message.ButtonId;

        core.Scheduler.NextWorldUpdate(() => DispatchClick(playerId, layoutHandle, buttonId));
        return HookResult.Continue;
    }

    private void DispatchClick(int playerId, uint packedLayoutHandle, string buttonId)
    {
        if (!_initialized ||
            !_sessions.TryGetValue(playerId, out var session) ||
            session.Runtime.Entity is not { IsValidEntity: true } expectedEntity)
        {
            return;
        }

        var clickedEntity = CHandle<CCSCustomHudLayout>
            .FromPackedInt((int)packedLayoutHandle)
            .Value;

        if (clickedEntity is null || clickedEntity.Index != expectedEntity.Index)
        {
            return;
        }

        if (!session.Runtime.Definition.Buttons.TryGetValue(buttonId, out var handler))
        {
            core.Logger.LogWarning(
                "[Menu] Неизвестная кнопка {ButtonId} в HUD-меню {MenuId}",
                buttonId,
                session.Runtime.Definition.Id
            );
            return;
        }

        var player = core.PlayerManager.GetPlayer(playerId);
        if (player is not { IsValid: true } || player.IsFakeClient)
        {
            Close(playerId);
            return;
        }

        try
        {
            handler(
                new HudMenuButtonContext(
                    player,
                    session.Runtime.Definition.Id,
                    buttonId,
                    session.View.State,
                    this
                )
            );
        }
        catch (Exception exception)
        {
            core.Logger.LogError(
                exception,
                "[Menu] Ошибка обработчика кнопки {ButtonId} в HUD-меню {MenuId}",
                buttonId,
                session.Runtime.Definition.Id
            );
        }
    }

    private void Unregister(MenuRuntime runtime)
    {
        if (!_menus.TryGetValue(runtime.Definition.Id, out var registered) || !ReferenceEquals(runtime, registered))
        {
            return;
        }

        foreach (var playerId in _sessions
                     .Where(pair => ReferenceEquals(pair.Value.Runtime, runtime))
                     .Select(pair => pair.Key)
                     .ToArray())
        {
            Close(playerId);
        }

        Despawn(runtime);
        _menus.Remove(runtime.Definition.Id);
    }

    private static void Despawn(MenuRuntime runtime)
    {
        var entity = runtime.Entity;
        runtime.Entity = null;

        if (entity is { IsValidEntity: true })
        {
            entity.Despawn();
        }
    }

    private void OnMapLoad(IOnMapLoadEvent @event) => ResetWorldState();

    private void OnMapUnload(IOnMapUnloadEvent @event) => ResetWorldState();

    private void OnClientDisconnected(IOnClientDisconnectedEvent @event) => Close(@event.PlayerId);

    private void ResetWorldState()
    {
        _sessions.Clear();

        foreach (var runtime in _menus.Values)
        {
            runtime.Entity = null;
        }
    }

    private sealed class MenuRuntime(DefinitionSnapshot definition)
    {
        public DefinitionSnapshot Definition { get; } = definition;

        public CCSCustomHudLayout? Entity { get; set; }
    }

    private sealed class MenuSession(MenuRuntime runtime, ViewSnapshot view)
    {
        public MenuRuntime Runtime { get; } = runtime;

        public ViewSnapshot View { get; set; } = view;
    }

    private sealed record DefinitionSnapshot(
        string Id,
        string LayoutPath,
        string RootPanelId,
        string OpenClassName,
        IReadOnlyDictionary<string, HudMenuButtonHandler> Buttons)
    {
        public static DefinitionSnapshot From(HudMenuDefinition definition) => new(
            definition.Id,
            definition.LayoutPath,
            definition.RootPanelId,
            definition.OpenClassName,
            new Dictionary<string, HudMenuButtonHandler>(definition.Buttons, StringComparer.Ordinal)
        );
    }

    private sealed record ViewSnapshot(
        IReadOnlyDictionary<(string PanelId, string VariableName), HudMenuDialogVariable> Variables,
        IReadOnlyDictionary<(string PanelId, string ClassName), HudMenuPanelClass> Classes,
        object? State)
    {
        public static ViewSnapshot From(HudMenuView view) => new(
            view.Variables.ToDictionary(variable => (variable.PanelId, variable.VariableName)),
            view.Classes.ToDictionary(panelClass => (panelClass.PanelId, panelClass.ClassName)),
            view.State
        );
    }

    private sealed class Registration(Action unregister) : IDisposable
    {
        private Action? _unregister = unregister;

        public void Dispose()
        {
            Interlocked.Exchange(ref _unregister, null)?.Invoke();
        }
    }
}
