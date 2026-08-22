using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Common.Database.Sessions;
using Common.Database.Storages;
using Common.Database.Tasks;
using Statistics.Core.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Statistics.Core.Services;

internal sealed class PlayerStatisticsService(
    ISwiftlyCore core,
    PlayerSessionStore<PlayerStatisticsState> sessions,
    IPlayerStatisticsPersistenceService persistenceService,
    DatabaseTaskTracker databaseTasks,
    SteamIdOperationQueue databaseOperations
)
{
    private const int AutosaveIntervalSeconds = 60;

    private const int PlayerNameMaxLength = 128;

    private CancellationTokenSource? _autosaveTimer;

    private int _isAutosaveRunning;

    public void Start()
    {
        if (_autosaveTimer is not null)
        {
            return;
        }

        _autosaveTimer = core.Scheduler.DelayAndRepeatBySeconds(
            delaySeconds: AutosaveIntervalSeconds,
            periodSeconds: AutosaveIntervalSeconds,
            task: QueueAutosave
        );
    }

    public void InitializeExistingPlayers()
    {
        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            Initialize(player, countSession: false);
        }
    }

    public void Initialize(IPlayer player, bool countSession = true)
    {
        if (!CanTrack(player))
        {
            return;
        }

        var steamId = player.SteamID;
        var session = sessions.Get(steamId);

        if (session is null)
        {
            session = sessions.Create(steamId, new PlayerStatisticsState());

            databaseTasks.Run(
                () => LoadAsync(steamId, session),
                $"Load player statistics {steamId}"
            );
        }

        var now = DateTime.UtcNow;
        var timestamp = Stopwatch.GetTimestamp();
        var playerName = NormalizePlayerName(player.Name);

        session.Update(data => data.Connect(playerName, now, timestamp, countSession));
    }

    public void Disconnect(IPlayer player, bool keepSession)
    {
        var steamId = player.SteamID;
        var session = sessions.Get(steamId);

        if (session is null)
        {
            return;
        }

        var now = DateTime.UtcNow;
        var timestamp = Stopwatch.GetTimestamp();
        var playerName = NormalizePlayerName(player.Name);

        session.Update(data => data.Disconnect(playerName, now, timestamp));

        if (keepSession)
        {
            QueueSave(steamId, session);

            return;
        }

        SaveAndRemove(steamId);
    }

    public void RecordDamageToZombies(ulong steamId, int damage)
    {
        Update(steamId, data => data.RecordDamageToZombies(damage));
    }

    public void RecordDamageToHumans(ulong steamId, int damage)
    {
        Update(steamId, data => data.RecordDamageToHumans(damage));
    }

    public void RecordZombieKill(ulong steamId, bool isHeadshot)
    {
        Update(steamId, data => data.RecordZombieKill(isHeadshot));
    }

    public void RecordInfection(ulong infectorSteamId, ulong infectedSteamId)
    {
        Update(infectorSteamId, static data => data.RecordInfectionMade());
        Update(infectedSteamId, static data => data.RecordTimesInfected());
    }

    public void RecordDeath(ulong steamId, PlayerRole role)
    {
        Update(steamId, data => data.RecordDeath(role));
    }

    public void RecordRound(ulong steamId, RoundStatisticsResult result)
    {
        Update(steamId, data => data.RecordRound(result));
    }

    public void ResetStreaks(ulong steamId)
    {
        Update(steamId, static data => data.ResetStreaks());
    }

    public void ResetAllStreaks()
    {
        foreach (var (_, session) in sessions.GetAll())
        {
            session.Update(static data => data.ResetStreaks());
        }
    }

    public void RemoveDisconnectedSessions()
    {
        foreach (var (steamId, session) in sessions.GetAll())
        {
            if (session.Read(static data => data.IsConnected))
            {
                continue;
            }

            SaveAndRemove(steamId);
        }
    }

    public void StopAndWait()
    {
        _autosaveTimer?.Cancel();
        _autosaveTimer = null;

        CheckpointConnectedSessions();

        foreach (var (steamId, _) in sessions.GetAll())
        {
            SaveAndRemove(steamId);
        }

        databaseTasks.StopAndWait();
    }

    private void QueueAutosave()
    {
        if (Interlocked.Exchange(ref _isAutosaveRunning, 1) != 0)
        {
            return;
        }

        CheckpointConnectedSessions();

        var activeSessions = sessions.GetAll();

        databaseTasks.Run(
            async () =>
            {
                try
                {
                    await Task.WhenAll(
                            activeSessions.Select(x => SaveAsync(x.Key, x.Value))
                        )
                        .ConfigureAwait(false);
                }
                finally
                {
                    Volatile.Write(ref _isAutosaveRunning, 0);
                }
            },
            "Autosave player statistics"
        );
    }

    private void CheckpointConnectedSessions()
    {
        var now = DateTime.UtcNow;
        var timestamp = Stopwatch.GetTimestamp();

        foreach (var (steamId, session) in sessions.GetAll())
        {
            if (!session.Read(static data => data.IsConnected))
            {
                continue;
            }

            var player = core.PlayerManager.GetPlayerFromSteamId(steamId, allowUnauthorized: false);
            var playerName = CanTrack(player)
                ? NormalizePlayerName(player.Name)
                : session.Read(static data => data.LastKnownName);

            session.Update(data => data.Checkpoint(playerName, now, timestamp));
        }
    }

    private void SaveAndRemove(ulong steamId)
    {
        if (!sessions.TryRemove(steamId, out var session) || session is null)
        {
            return;
        }

        QueueSave(steamId, session);
    }

    private void QueueSave(ulong steamId, PersistentSession<PlayerStatisticsState> session)
    {
        databaseTasks.Run(
            () => SaveAsync(steamId, session),
            $"Save player statistics {steamId}"
        );
    }

    private Task LoadAsync(
        ulong steamId,
        PersistentSession<PlayerStatisticsState> session,
        CancellationToken cancellationToken = default
    )
    {
        return databaseOperations.RunAsync(
            steamId,
            () => EnsureLoadedAsync(steamId, session, cancellationToken)
        );
    }

    private Task SaveAsync(
        ulong steamId,
        PersistentSession<PlayerStatisticsState> session,
        CancellationToken cancellationToken = default
    )
    {
        return databaseOperations.RunAsync(
            steamId,
            async () =>
            {
                await session.SaveLock
                    .WaitAsync(cancellationToken)
                    .ConfigureAwait(false);

                try
                {
                    await EnsureLoadedAsync(steamId, session, cancellationToken)
                        .ConfigureAwait(false);

                    var snapshot = session.CreateSnapshot(static data => data.CreateSnapshot());

                    if (!snapshot.IsDirty)
                    {
                        return;
                    }

                    await persistenceService
                        .SaveAsync(steamId, snapshot.Data, cancellationToken)
                        .ConfigureAwait(false);

                    session.MarkSaved(snapshot.Revision);
                }
                finally
                {
                    session.SaveLock.Release();
                }
            }
        );
    }

    private async Task EnsureLoadedAsync(
        ulong steamId,
        PersistentSession<PlayerStatisticsState> session,
        CancellationToken cancellationToken
    )
    {
        var state = session.CreateSnapshot(static _ => true);

        if (state.IsLoaded)
        {
            return;
        }

        var loaded = await persistenceService
            .LoadAsync(steamId, cancellationToken)
            .ConfigureAwait(false);

        if (loaded is null)
        {
            session.CompleteLoadAsNew();

            return;
        }

        session.CompleteLoadMerged(data => data.Merge(loaded));
    }

    private void Update(ulong steamId, Action<PlayerStatisticsState> update)
    {
        var session = sessions.Get(steamId);

        session?.Update(update);
    }

    private static bool CanTrack([NotNullWhen(true)] IPlayer? player)
    {
        return player is
        {
            IsValid: true,
            IsAuthorized: true,
            IsFakeClient: false
        } && player.SteamID != 0;
    }

    private static string NormalizePlayerName(string? playerName)
    {
        var normalized = string.IsNullOrWhiteSpace(playerName)
            ? "Unknown"
            : playerName.Trim();

        return normalized.Length <= PlayerNameMaxLength
            ? normalized
            : TruncatePlayerName(normalized);
    }

    private static string TruncatePlayerName(string playerName)
    {
        var length = PlayerNameMaxLength;

        if (char.IsHighSurrogate(playerName[length - 1]) &&
            char.IsLowSurrogate(playerName[length]))
        {
            length--;
        }

        return playerName[..length];
    }
}
