using Localization.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using ZombiePlague.Core.Data.Managers.Contracts;

namespace ZombiePlague.Core.Experimental.AbilityHud;

internal sealed class AbilityHudService(ISwiftlyCore core, IPlayerManager players, IOptions<AbilityHudConfig> options,
    Func<ILocalizationApi> localization) : IDisposable
{
    private CustomHudRuntime? _runtime;
    private AbilityHudPresenter? _presenter;
    private CancellationTokenSource? _timer;
    private AbilityHudConfig _config = new();
    private Guid? _command;
    private bool _started;
    private bool _disposed;
    private bool _wanted;
    private int _generation;
    private string _status = "выключен";

    public void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnClientDisconnected += OnDisconnect;
        _command = core.Command.RegisterCommand("zp_ability_hud", Command, registerRaw: true, permission: "zombie_plague.admin.classes");
        try { _config = options.Value; _config.Validate(); _wanted = _config.Enabled; }
        catch (Exception error) { Fault(error); }
        if (_wanted) QueueStart();
    }

    private void Command(ICommandContext context)
    {
        switch (context.Args.FirstOrDefault()?.ToLowerInvariant())
        {
            case "on": _wanted = true; QueueStart(); break;
            case "off": _wanted = false; Stop(); _status = "выключен"; break;
            case null or "status": break;
            default: context.Reply("zp_ability_hud on | off | status"); return;
        }
        context.Reply($"Ability HUD: {_status}; requested={_wanted}; api={CustomHudRuntime.HasRequiredApi}; players={_presenter?.PlayerCount ?? 0}");
    }

    private void QueueStart()
    {
        Stop();
        _status = "запуск на следующем кадре";
        var generation = _generation;
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (_disposed || !_wanted || generation != _generation) return;
            try
            {
                _config.Validate();
                _runtime = CustomHudRuntime.Create(core);
                _presenter = new AbilityHudPresenter(_runtime);
                // Таймер SwiftlyS2 выполняется по игровым тикам, вызовы HUD остаются на игровом потоке
                _timer = core.Scheduler.DelayAndRepeatBySeconds(_config.RefreshSeconds, _config.RefreshSeconds, Tick);
                _status = "работает";
                core.Logger.LogInformation("[AbilityHud] Эксперимент включён: способности текущей роли, период {Interval} с", _config.RefreshSeconds);
            }
            catch (Exception error) { Fault(error); }
        });
    }

    private void Tick()
    {
        if (_disposed || !_wanted || _presenter is null || _runtime is null) return;
        try
        {
            if (!_runtime.IsValid) throw new InvalidOperationException("Сущность HUD удалена — включите эксперимент повторно");
            var seen = new HashSet<int>();
            foreach (var player in players.GetAllPlayers())
            {
                if (!player.IsValid || player.IsFakeClient) continue;
                seen.Add(player.PlayerID);
                if (!player.IsAlive || (_config.HideWhenMenuOpen && core.MenusAPI.GetCurrentMenu(player) is not null)
                    || !players.TryGetRole(player, out var role))
                {
                    _presenter.Render(player.PlayerID, AbilityHudFrame.Empty);
                    continue;
                }
                _presenter.Render(player.PlayerID, AbilityHudFrame.ForRole(role,
                    key => localization().GetForPlayerOrKey(player, key), _config.ShowNames));
            }
            foreach (var playerId in _presenter.PlayerIds.Where(id => !seen.Contains(id)).ToArray()) _presenter.Clear(playerId);
        }
        catch (Exception error) { Fault(error); }
    }

    private void OnMapLoad(IOnMapLoadEvent args) { if (_wanted) QueueStart(); }
    private void OnMapUnload(IOnMapUnloadEvent args) { Stop(); _status = "карта выгружена"; }
    private void OnDisconnect(IOnClientDisconnectedEvent args)
    {
        if (_presenter is null || _disposed) return;
        try { _presenter.Clear(args.PlayerId); }
        catch (Exception error) { Fault(error); }
    }

    private void Fault(Exception error)
    {
        _wanted = false;
        Stop();
        _status = error.Message;
        core.Logger.LogWarning(error, "[AbilityHud] Эксперимент остановлен: {Reason}", error.Message);
    }

    private void Stop()
    {
        _generation++;
        _timer?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _presenter = null;
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
