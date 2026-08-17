using Common.Database.Sessions;
using Common.Database.Storages;
using Common.Database.Tasks;
using CustomKnife.Data.Knives;
using CustomKnife.Data.Services.Contracts;
using CustomKnife.Data.Store;

namespace CustomKnife.Data.Services;

internal sealed class PlayerKnifeService(
    PlayerSessionStore<PlayerKnifePreferences> sessions,
    IPlayerKnifePersistenceService persistenceService,
    DatabaseTaskTracker databaseTasks,
    SteamIdOperationQueue databaseOperations
) : IPlayerKnifeService
{
    public void Initialize(ulong steamId)
    {
        var preferences = new PlayerKnifePreferences
        {
            KnifeId = KnifeDefaults.DefaultKnifeId
        };

        var session = sessions.Create(steamId, preferences);

        databaseTasks.Run(
            () => InitializeAsync(
                steamId,
                session
            )
        );
    }

    public string? GetKnifeId(ulong steamId)
    {
        return sessions
            .Get(steamId)?
            .Read(data => data.KnifeId);
    }

    public void SetKnifeId(
        ulong steamId,
        string knifeId)
    {
        sessions
            .Get(steamId)?
            .Update(
                data =>
                {
                    data.KnifeId = knifeId;
                }
            );
    }

    public void Remove(ulong steamId)
    {
        if (!sessions.TryRemove(steamId, out var session) || session is null)
        {
            return;
        }

        databaseTasks.Run(
            () => SaveOnDisconnectAsync(
                steamId,
                session
            )
        );
    }

    private Task InitializeAsync(
        ulong steamId,
        PersistentSession<PlayerKnifePreferences> session,
        CancellationToken cancellationToken = default)
    {
        return databaseOperations.RunAsync(
            steamId,
            async () =>
            {
                var databaseKnifeId = await persistenceService
                        .LoadAsync(steamId, cancellationToken)
                        .ConfigureAwait(false);

                if (databaseKnifeId is null)
                {
                    session.CompleteLoadAsNew();

                    return;
                }

                session.CompleteLoad(
                    data =>
                    {
                        data.KnifeId = databaseKnifeId;
                    }
                );
            }
        );
    }

    private Task SaveOnDisconnectAsync(
        ulong steamId,
        PersistentSession<PlayerKnifePreferences> session,
        CancellationToken cancellationToken = default)
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
                    var snapshot = session.CreateSnapshot(
                        data => data.KnifeId
                    );

                    if (!snapshot.IsLoaded || !snapshot.IsDirty)
                    {
                        return;
                    }

                    await persistenceService
                        .SaveAsync(steamId, snapshot.Data, cancellationToken)
                        .ConfigureAwait(false);

                    session.MarkSaved(
                        snapshot.Revision
                    );
                }
                finally
                {
                    session.SaveLock.Release();
                }
            }
        );
    }
}