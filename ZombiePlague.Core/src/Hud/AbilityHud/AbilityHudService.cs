using System.Diagnostics;
using Localization.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Data.Managers.Contracts;

namespace ZombiePlague.Core.Hud.AbilityHud;

internal sealed class AbilityHudService(ISwiftlyCore core, IPlayerManager players, IOptions<AbilityHudConfig> options,
    Func<ILocalizationApi> localization, AbilityHudSettings settings, Func<IAbilityHudRuntime> createRuntime) : IDisposable
{
    private const int MaximumEntityRecoveries = 3;
    public bool IsRunning => !_disposed && _wanted && _presenter is not null;
    private IAbilityHudRuntime? _runtime;
    private AbilityHudPresenter? _presenter;
    private CancellationTokenSource? _timer;
    private AbilityHudConfig _config = new();
    private Guid? _command;
    private bool _started;
    private bool _disposed;
    private bool _wanted;
    private bool _mapUnloading;
    private int _entityRecoveries;
    private readonly HashSet<int> _failedPlayers = [];
    private int _generation;
    private string _status = "выключен";
    private long _ticks;
    private long _lastTickTimestamp;

    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnClientDisconnected += OnDisconnect;
        _command = core.Command.RegisterCommand("zp_ability_hud", Command, registerRaw: true, permission: "zombie_plague.admin.classes");
        try { _config = options.Value; _wanted = _config.Enabled; _config.Validate(); }
        catch (Exception error) { Fault(error); return; }
        if (_wanted) QueueStart();
    }

    private void Command(ICommandContext context)
    {
        switch (context.Args.FirstOrDefault()?.ToLowerInvariant())
        {
            case "on": _wanted = true; QueueStart(); break;
            case "off": _wanted = false; Stop(); _status = "выключен"; break;
            case "debug":
                ReplyStatus(context, diagnostic: true);
                ReplyDiagnostics(context);
                if (context.IsSentByPlayer) context.Reply("Ability HUD: диагностика отправлена в консоль игры (~)");
                return;
            case null or "status": break;
            default: context.Reply("zp_ability_hud on | off | status | debug"); return;
        }
        ReplyStatus(context);
    }

    private void ReplyStatus(ICommandContext context, bool diagnostic = false)
    {
        var connected = core.PlayerManager.GetAllPlayers().Count(player => player.IsValid && !player.IsFakeClient);
        var tracked = players.GetAllPlayers().Count(player => player.IsValid && !player.IsFakeClient);
        var age = _ticks == 0 ? "never" : ((long)Stopwatch.GetElapsedTime(_lastTickTimestamp).TotalMilliseconds).ToString();
        Reply($"Ability HUD: {_status}; requested={_wanted}; api={CustomHudRuntime.HasRequiredApi}");
        Reply($"Ability HUD: connected={connected}; tracked={tracked}; recipients={_presenter?.PlayerCount ?? 0}; ticks={_ticks}; last_tick_ms={age}");
        void Reply(string message)
        {
            if (diagnostic) ReplyDiagnostic(context, message);
            else context.Reply(message);
        }
    }

    private void ReplyDiagnostics(ICommandContext context)
    {
        // Проверяем также клиентов без роли ZombiePlague: основной цикл пока не включает их в свой обход
        var tracked = players.GetAllPlayers().Where(player => player.IsValid && !player.IsFakeClient).ToArray();
        var connected = core.PlayerManager.GetAllPlayers().Where(player => player.IsValid && !player.IsFakeClient
            && (!context.IsSentByPlayer || player.PlayerID == context.Sender?.PlayerID)).ToArray();
        if (connected.Length == 0) ReplyDiagnostic(context, "Ability HUD debug: подходящих подключённых игроков нет");
        foreach (var client in connected)
        {
            var player = tracked.FirstOrDefault(owner => owner.PlayerID == client.PlayerID && owner.SteamID == client.SteamID) ?? client;
            players.TryGetRole(player, out var role);
            var menu = core.MenusAPI.GetCurrentMenu(player);
            var frame = AbilityHudFrame.ForPlayer(player, role, _config,
                key => localization().GetForPlayerOrKey(player, key), out var visibility);
            var abilities = AbilityHudFrame.AbilitiesForRole(role);
            var roleName = role switch { IHuman => "human", IZombie => "zombie", null => "none", _ => role.GetType().Name };
            var className = role switch { IZombie zombie => zombie.ZClass.InternalName, IHuman human => human.HClass.GetType().Name, _ => "none" };
            var ids = abilities.Select(ability => ability is IPresentedAbility { Presentation: { } info }
                ? info.Key : "missing:" + ability.GetType().Name).ToArray();
            var missing = abilities.Count(ability => ability is not IPresentedAbility { Presentation: not null });
            var menuName = menu is null ? "none" : "open";
            var appearance = settings.Get(player.SteamID);
            ReplyDiagnostic(context, $"Ability HUD debug: slot={player.PlayerID}; steam={player.SteamID}; alive={player.IsAlive}; role={roleName}; class={className}");
            ReplyDiagnostic(context, $"Ability HUD debug: slot={player.PlayerID}; reason={visibility}; abilities={abilities.Count}; missing_metadata={missing}; menu={menuName}; eligible_icons={frame.Icons.Length}; sent_icons={_presenter?.GetIconCount(player.PlayerID) ?? 0}");
            ReplyDiagnostic(context, $"Ability HUD debug: slot={player.PlayerID}; scale={appearance.ScalePercent}; position={appearance.Position}");
            foreach (var group in ids.Chunk(4))
                ReplyDiagnostic(context, $"Ability HUD debug: slot={player.PlayerID}; ids=[{string.Join(",", group)}]");
            foreach (var group in frame.Icons.Chunk(4))
                ReplyDiagnostic(context, $"Ability HUD debug: slot={player.PlayerID}; icons=[{string.Join(",", group.Select(icon => $"{icon.Key}:Kind_{icon.Kind}:{icon.State}"))}]");
        }
        ReplyDiagnostic(context, "Ready означает готовность набора на сервере; получение и отрисовку Panorama клиентом сервер не подтверждает");
    }

    private static void ReplyDiagnostic(ICommandContext context, string message)
    {
        // Полный отчёт отправляем в консоль: чат обрезает длинные списки и мешает игре
        if (context.IsSentByPlayer) context.Sender?.SendMessage(MessageType.Console, message + Environment.NewLine);
        else context.Reply(message);
    }

    private void QueueStart(bool resetRecovery = true)
    {
        Stop();
        if (resetRecovery) _entityRecoveries = 0;
        if (_mapUnloading) { _status = "ожидание загрузки карты"; return; }
        _status = "запуск на следующем кадре";
        var generation = _generation;
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (_disposed || !_wanted || generation != _generation) return;
            try
            {
                _config.Validate();
                _runtime = createRuntime();
                _presenter = new AbilityHudPresenter(_runtime);
                // Таймер SwiftlyS2 выполняется по игровым тикам, вызовы HUD остаются на игровом потоке
                _timer = core.Scheduler.DelayAndRepeatBySeconds(_config.RefreshSeconds, _config.RefreshSeconds, () =>
                {
                    if (generation == _generation) Tick();
                });
                _status = "работает";
                core.Logger.LogInformation("[AbilityHud] Панель включена: способности текущей роли, период {Interval} с", _config.RefreshSeconds);
            }
            catch (Exception error) { Fault(error); }
        });
    }

    private void Tick()
    {
        if (_disposed || !_wanted || _presenter is null || _runtime is null) return;
        try
        {
            if (!_runtime.IsValid)
            {
                if (_entityRecoveries >= MaximumEntityRecoveries)
                    throw new InvalidOperationException("Сущность HUD повторно удаляется — проверьте карту и плагины, затем выполните zp_ability_hud on");
                _entityRecoveries++;
                core.Logger.LogWarning("[AbilityHud] Сущность удалена, восстановление {Attempt}/{Maximum}", _entityRecoveries, MaximumEntityRecoveries);
                QueueStart(resetRecovery: false);
                return;
            }
            var seen = new HashSet<int>();
            foreach (var player in players.GetAllPlayers())
            {
                if (!player.IsValid || player.IsFakeClient) continue;
                seen.Add(player.PlayerID);
                var frame = BuildFrame(player);
                _presenter.Render(player.PlayerID, frame);
            }
            foreach (var playerId in _presenter.PlayerIds.Where(id => !seen.Contains(id)).ToArray()) _presenter.Clear(playerId);
            _ticks++;
            _lastTickTimestamp = Stopwatch.GetTimestamp();
        }
        catch (Exception error) { Fault(error); }
    }

    private AbilityHudFrame BuildFrame(IPlayer player)
    {
        try
        {
            players.TryGetRole(player, out var role);
            var frame = AbilityHudFrame.ForPlayer(player, role, _config,
                key => localization().GetForPlayerOrKey(player, key), out var visibility);
            if (visibility is AbilityHudVisibility.Ready or AbilityHudVisibility.NoAbilities or AbilityHudVisibility.MissingPresentation)
                frame = frame with { Appearance = settings.Get(player.SteamID) };
            _failedPlayers.Remove(player.PlayerID);
            return frame;
        }
        catch (Exception error)
        {
            // Ошибка одной роли скрывает только её панель; повторяющуюся ошибку логируем один раз до восстановления
            if (_failedPlayers.Add(player.PlayerID))
                core.Logger.LogWarning(error, "[AbilityHud] Не удалось подготовить панель игрока {PlayerId}", player.PlayerID);
            return AbilityHudFrame.Empty;
        }
    }

    private void OnMapLoad(IOnMapLoadEvent args) { _mapUnloading = false; if (_wanted && !_disposed) QueueStart(); }
    private void OnMapUnload(IOnMapUnloadEvent args) { _mapUnloading = true; Stop(); _status = "карта выгружена"; }
    private void OnDisconnect(IOnClientDisconnectedEvent args)
    {
        _failedPlayers.Remove(args.PlayerId);
        if (_presenter is null || _disposed) return;
        try { _presenter.Clear(args.PlayerId); }
        catch (Exception error) { Fault(error); }
    }

    private void Fault(Exception error)
    {
        Stop();
        _status = error.Message;
        core.Logger.LogWarning(error, "[AbilityHud] Панель остановлена: {Reason}. Повторный запуск при загрузке карты или через zp_ability_hud on", error.Message);
    }

    private void Stop()
    {
        _generation++;
        _timer?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _ticks = 0;
        _lastTickTimestamp = 0;
        _presenter = null;
        _failedPlayers.Clear();
        var runtime = _runtime;
        _runtime = null;
        try { runtime?.Dispose(); }
        catch (Exception error) { core.Logger.LogWarning(error, "[AbilityHud] Ошибка удаления сущности HUD"); }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _wanted = false;
        Stop();
        if (!_started) return;
        core.Event.OnMapLoad -= OnMapLoad;
        core.Event.OnMapUnload -= OnMapUnload;
        core.Event.OnClientDisconnected -= OnDisconnect;
        if (_command is { } command) core.Command.UnregisterCommand(command);
    }
}
