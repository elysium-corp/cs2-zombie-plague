using CustomHud.Api;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Players;

namespace CustomHud.Core;

internal sealed class CustomHudService(ISwiftlyCore core, IOptions<CustomHudConfig> config,
    TimeProvider clock, Func<IHudRuntime> createRuntime) : ICustomHudApi, IDisposable
{
    private static readonly HudPosition[] Positions = Enum.GetValues<HudPosition>();
    private readonly HudMessageStore _messages = new(clock);
    private IHudRuntime? _runtime;
    private HudPresenter? _presenter;
    private CancellationTokenSource? _timer;
    private Guid? _command;
    private bool _started;
    private bool _disposed;
    private bool _wanted;
    private bool _mapUnloading;
    private int _generation;
    private int _recoveries;
    private string _status = "выключен";

    public bool IsAvailable => !_disposed && _wanted && _runtime is { IsValid: true };

    internal void Start()
    {
        if (_started || _disposed) return;
        _started = true;
        _wanted = config.Value.Enabled;
        core.Event.OnMapLoad += OnMapLoad;
        core.Event.OnMapUnload += OnMapUnload;
        core.Event.OnClientConnected += OnConnect;
        core.Event.OnClientDisconnected += OnDisconnect;
        _command = core.Command.RegisterCommand("custom_hud", Command, registerRaw: true, permission: "custom_hud.admin");
        if (_wanted) QueueStart();
    }

    public bool Show(IPlayer player, string text, HudMessageOptions? options = null)
    {
        options ??= new HudMessageOptions();
        HudMessageStore.Validate(options);
        ArgumentNullException.ThrowIfNull(text);
        if (!IsAvailable || !Eligible(player) || string.IsNullOrWhiteSpace(text)) return false;
        var document = HudMarkup.Parse(text, options.Format, options.Style);
        return document.Lines.Any(line => line.Any(run => !string.IsNullOrWhiteSpace(run.Text)))
            && _messages.Put(player.PlayerID, player.SteamID, document, options);
    }

    public int Broadcast(string text, HudMessageOptions? options = null)
    {
        options ??= new HudMessageOptions();
        HudMessageStore.Validate(options);
        ArgumentNullException.ThrowIfNull(text);
        if (!IsAvailable || string.IsNullOrWhiteSpace(text)) return 0;
        var document = HudMarkup.Parse(text, options.Format, options.Style);
        if (!document.Lines.Any(line => line.Any(run => !string.IsNullOrWhiteSpace(run.Text)))) return 0;
        var count = 0;
        foreach (var player in core.PlayerManager.GetAllPlayers().Where(Eligible))
            if (_messages.Put(player.PlayerID, player.SteamID, document, options)) count++;
        return count;
    }

    public void Hide(IPlayer player, string channel)
    {
        if (_disposed || !Eligible(player)) return;
        _messages.Hide(player.PlayerID, player.SteamID, channel);
    }

    public void ClearChannel(string channel)
    {
        if (!_disposed) _messages.ClearChannel(channel);
    }

    private static bool Eligible(IPlayer player) => player is { IsValid: true, IsFakeClient: false };

    private void QueueStart(bool preserveMessages = false)
    {
        Stop(clearMessages: !preserveMessages);
        if (_mapUnloading) { _status = "ожидание карты"; return; }
        _status = "запуск на следующем кадре";
        var generation = _generation;
        core.Scheduler.NextWorldUpdate(() =>
        {
            if (_disposed || !_wanted || generation != _generation) return;
            try
            {
                _runtime = createRuntime();
                _presenter = new HudPresenter(_runtime);
                _timer = core.Scheduler.DelayAndRepeatBySeconds(0.1f, 0.1f, () =>
                {
                    if (generation == _generation) Tick();
                });
                _status = "работает";
                core.Logger.LogInformation("[CustomHud] API сообщений и баннеров включён");
            }
            catch (Exception error) { Fault(error); }
        });
    }

    private void Tick()
    {
        if (_disposed || !_wanted || _runtime is null || _presenter is null) return;
        try
        {
            if (!_runtime.IsValid)
            {
                if (_recoveries++ >= 3) throw new InvalidOperationException("Сущность HUD удаляется повторно; проверьте карту и плагины");
                QueueStart(preserveMessages: true);
                return;
            }
            var seen = new HashSet<int>();
            foreach (var player in core.PlayerManager.GetAllPlayers().Where(Eligible))
            {
                seen.Add(player.PlayerID);
                var frame = _messages.GetFrame(player.PlayerID, player.SteamID);
                foreach (var position in Positions)
                    _presenter.Render(player.PlayerID, player.SteamID, position, frame[(int)position]);
            }
            foreach (var id in _presenter.PlayerIds.Where(id => !seen.Contains(id))) ClearPlayer(id);
        }
        catch (Exception error) { Fault(error); }
    }

    private void OnMapLoad(IOnMapLoadEvent args)
    {
        _mapUnloading = false;
        _recoveries = 0;
        if (_wanted && !_disposed) QueueStart();
    }

    private void OnMapUnload(IOnMapUnloadEvent args) { _mapUnloading = true; Stop(); _status = "карта выгружена"; }
    private void OnConnect(IOnClientConnectedEvent args) => ClearPlayer(args.PlayerId);
    private void OnDisconnect(IOnClientDisconnectedEvent args) => ClearPlayer(args.PlayerId);

    private void ClearPlayer(int playerId)
    {
        _messages.Disconnect(playerId);
        if (_disposed || _runtime is not { IsValid: true }) return;
        try { _presenter?.Clear(playerId); }
        catch (Exception error) { Fault(error); }
    }

    private void Command(ICommandContext context)
    {
        switch (context.Args.FirstOrDefault()?.ToLowerInvariant())
        {
            case "on": _wanted = true; _recoveries = 0; QueueStart(); break;
            case "off": _wanted = false; Stop(); _status = "выключен"; break;
            case "test":
                var position = HudPosition.TopCenter;
                if (context.Args.Length > 1 && (!Enum.TryParse(context.Args[1], true, out position) || !Enum.IsDefined(position)))
                {
                    context.Reply("Позиции: " + string.Join(", ", Positions));
                    return;
                }
                const string sample = "<font color='mint'><b>ELYSIUM</b></font><br>" +
                    "<font color='gold'>Массовое заражение</font><br><i>Custom HUD готов</i>";
                var options = new HudMessageOptions { Channel = "CustomHud.Test", Position = position, Style = HudMessageStyle.Banner, Priority = 100 };
                var count = context.IsSentByPlayer && context.Sender is { } sender
                    ? (Show(sender, sample, options) ? 1 : 0) : Broadcast(sample, options);
                context.Reply($"Custom HUD: сообщение принято для {count} игроков");
                return;
            case null or "status": break;
            default: context.Reply("custom_hud on | off | status | test [Position]"); return;
        }
        context.Reply($"Custom HUD: {_status}; enabled={_wanted}; available={IsAvailable}");
    }

    private void Fault(Exception error)
    {
        Stop();
        _status = error.Message;
        core.Logger.LogWarning(error, "[CustomHud] HUD остановлен: {Reason}; повторный запуск на следующей карте или custom_hud on", error.Message);
    }

    private void Stop(bool clearMessages = true)
    {
        _generation++;
        _timer?.Cancel();
        _timer?.Dispose();
        _timer = null;
        _presenter = null;
        if (clearMessages) _messages.Clear();
        var runtime = _runtime;
        _runtime = null;
        try { runtime?.Dispose(); }
        catch (Exception error) { core.Logger.LogWarning(error, "[CustomHud] Не удалось удалить сущность HUD"); }
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
        core.Event.OnClientConnected -= OnConnect;
        core.Event.OnClientDisconnected -= OnDisconnect;
        if (_command is { } command) core.Command.UnregisterCommand(command);
    }
}
