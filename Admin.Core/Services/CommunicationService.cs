using Admin.Core.Data;
using Common.Database.Tasks;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Events;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Services;

internal sealed class CommunicationService(ISwiftlyCore core, CommunicationBlockRepository repository,
    DatabaseTaskTracker tasks, ILogger<CommunicationService> logger) : IDisposable
{
    private readonly CommunicationBlockState _state = new();
    private readonly Dictionary<ulong, bool> _previousMute = [];
    private readonly SemaphoreSlim _writes = new(1, 1);
    private Guid _chatHook;
    private CancellationTokenSource? _timer;
    private bool _started;
    private bool _available;

    public void Start()
    {
        if (_started) return;
        _started = true;
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
            foreach (var block in repository.LoadAsync(timeout.Token).GetAwaiter().GetResult())
                _state.Set((ulong)block.SteamId, block.Kind, block.ExpiresAtUtc);
            _available = true;
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to load communication blocks");
        }

        _chatHook = core.Command.HookClientChat(OnChat);
        core.Event.OnClientSteamAuthorize += OnAuthorize;
        core.Event.OnClientDisconnected += OnDisconnect;
        _timer = core.Scheduler.RepeatBySeconds(0.25f, UpdatePlayers);
        UpdatePlayers();
    }

    public bool IsBlocked(IPlayer player, CommunicationKind kind) =>
        _state.IsBlocked(player.SteamID, kind, DateTime.UtcNow);

    public bool Change(ulong steamId, CommunicationKind kind, ulong? administrator,
        int minutes, string reason, bool remove, Action<bool> completed)
    {
        if (!_started || !_available || steamId == 0 || minutes < 0 || minutes > 525600 || reason.Length > 256)
            return false;
        DateTime? expiresAt = minutes == 0 ? null : DateTime.UtcNow.AddMinutes(minutes);
        tasks.Run(async cancellationToken =>
        {
            await _writes.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await repository.SaveAsync(steamId, kind, administrator, expiresAt, reason, remove, cancellationToken)
                    .ConfigureAwait(false);
                await core.Scheduler.NextTickAsync(() =>
                {
                    if (!_started) return;
                    if (remove) _state.Remove(steamId, kind);
                    else _state.Set(steamId, kind, expiresAt);
                    UpdatePlayers();
                    completed(true);
                }).ConfigureAwait(false);
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Failed to update {Kind} for {SteamId}", kind, steamId);
                await core.Scheduler.NextTickAsync(() =>
                {
                    if (_started) completed(false);
                }).ConfigureAwait(false);
            }
            finally
            {
                _writes.Release();
            }
        }, $"Update {kind} for {steamId}");
        return true;
    }

    private HookResult OnChat(int playerId, string text, bool teamOnly)
    {
        var player = core.PlayerManager.GetPlayer(playerId);
        return player is { IsValid: true } && IsBlocked(player, CommunicationKind.Gag)
            ? HookResult.Stop : HookResult.Continue;
    }

    private void UpdatePlayers()
    {
        if (!_started) return;
        foreach (var player in core.PlayerManager.GetAllValidPlayers()) UpdateVoice(player);
    }

    private void UpdateVoice(IPlayer player)
    {
        if (IsBlocked(player, CommunicationKind.Mute))
        {
            _previousMute.TryAdd(player.SessionId, (player.VoiceFlags & VoiceFlagValue.Muted) != 0);
            player.VoiceFlags |= VoiceFlagValue.Muted;
        }
        else RestoreVoice(player);
    }

    private void RestoreVoice(IPlayer player)
    {
        if (_previousMute.Remove(player.SessionId, out var wasMuted) && !wasMuted)
            player.VoiceFlags &= ~VoiceFlagValue.Muted;
    }

    private void OnAuthorize(IOnClientSteamAuthorizeEvent args)
    {
        if (core.PlayerManager.GetPlayer(args.PlayerId) is { IsValid: true } player) UpdateVoice(player);
    }

    private void OnDisconnect(IOnClientDisconnectedEvent args)
    {
        // Сессии, уже удалённые из PlayerManager, очищаются без обращения к их native-объектам.
        var sessions = core.PlayerManager.GetAllValidPlayers()
            .Where(player => player.PlayerID != args.PlayerId).Select(player => player.SessionId).ToHashSet();
        foreach (var session in _previousMute.Keys.Where(session => !sessions.Contains(session)).ToArray())
            _previousMute.Remove(session);
    }

    public void Dispose()
    {
        if (!_started) return;
        _started = false;
        _timer?.Cancel();
        _timer = null;
        core.Command.UnhookClientChat(_chatHook);
        core.Event.OnClientSteamAuthorize -= OnAuthorize;
        core.Event.OnClientDisconnected -= OnDisconnect;
        foreach (var player in core.PlayerManager.GetAllValidPlayers()) RestoreVoice(player);
        _previousMute.Clear();
        _state.Clear();
    }
}
