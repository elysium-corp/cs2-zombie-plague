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
    private const int PlayerNameMaxLength = 128;

    public void InitializeExistingPlayers()
    {
        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            Initialize(player);
        }
    }

    public void Initialize(IPlayer player)
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

        session.Update(data => data.Connect(playerName, now, timestamp));
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

    public void RecordZombieKill(ulong steamId, long currentStreak)
    {
        Update(steamId, data => data.RecordZombieKill(currentStreak));
    }

    public void RecordInfection(
        ulong infectorSteamId,
        ulong infectedSteamId,
        long currentInfectionStreak
    )
    {
        Update(
            infectorSteamId,
            data => data.RecordInfectionMade(currentInfectionStreak)
        );

        Update(infectedSteamId, static data => data.RecordTimesInfected());
    }

    public void RecordDeath(ulong steamId)
    {
        Update(steamId, static data => data.RecordDeath());
    }

    public long RecordRound(ulong steamId, RoundStatisticsResult result)
    {
        var session = sessions.Get(steamId);

        if (session is null)
        {
            return 0;
        }

        var appliedPointsDelta = 0L;

        session.Update(data => appliedPointsDelta = data.RecordRound(result));

        return appliedPointsDelta;
    }

    public void SaveRound()
    {
        foreach (var (steamId, session) in sessions.GetAll())
        {
            if (session.Read(static data => data.IsConnected))
            {
                QueueSave(steamId, session);
            }
            else
            {
                SaveAndRemove(steamId);
            }
        }
    }

    public void CheckpointAndSaveAll()
    {
        CheckpointConnectedSessions();

        foreach (var (steamId, session) in sessions.GetAll())
        {
            QueueSave(steamId, session);
        }
    }

    public void StopAndWait()
    {
        CheckpointConnectedSessions();

        foreach (var (steamId, _) in sessions.GetAll())
        {
            SaveAndRemove(steamId);
        }

        databaseTasks.StopAndWait();
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

            var player = core.PlayerManager.GetPlayerFromSteamId(
                steamId,
                allowUnauthorized: false
            );

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

    private void QueueSave(
        ulong steamId,
        PersistentSession<PlayerStatisticsState> session
    )
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

                    var snapshot = session.CreateSnapshot(
                        static data => data.CreateSnapshot()
                    );

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
        sessions.Get(steamId)?.Update(update);
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
