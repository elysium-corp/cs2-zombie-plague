using Common.Database.Sessions;
using Common.Database.Storages;
using Common.Database.Tasks;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Coordinators.Contracts;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Data.Coordinators;

internal sealed class PlayerPreferencesCoordinator(
    ISwiftlyCore core,
    IPlayerManager playerManager,
    PlayerSessionStore<PlayerPreferences> sessions,
    IPlayerPersistenceService playerPersistenceService,
    DatabaseTaskTracker databaseTasks
) : IPlayerPreferencesCoordinator
{
    public void Initialize(IPlayer player)
    {
        if (!CanInitialize(player))
        {
            return;
        }

        var steamId = player.SteamID;

        var session = sessions.Create(
            steamId,
            new PlayerPreferences()
        );

        databaseTasks.Run(
            () => LoadAsync(steamId, session),
            $"Load player preferences {steamId}"
        );
    }

    public void SaveAndRemove(IPlayer player)
    {
        var steamId = player.SteamID;

        if (!sessions.TryRemove(steamId, out var session) || session is null)
        {
            return;
        }

        if (!CanSave(player))
        {
            return;
        }

        databaseTasks.Run(
            () => SaveAsync(steamId, session),
            $"Save player preferences {steamId}"
        );
    }

    public void SaveAllAndWait()
    {
        foreach (var (steamId, _) in sessions.GetAll())
        {
            if (!sessions.TryRemove(steamId, out var session) || session is null)
            {
                continue;
            }

            databaseTasks.Run(() => SaveAsync(steamId, session), $"Save player preferences {steamId}");
        }

        databaseTasks.StopAndWait();
    }

    private async Task LoadAsync(ulong steamId, PersistentSession<PlayerPreferences> session, CancellationToken cancellationToken = default)
    {
        var loaded = await playerPersistenceService
            .LoadAsync(steamId)
            .ConfigureAwait(false);

        if (!sessions.IsCurrent(steamId, session))
        {
            return;
        }

        if (loaded is null)
        {
            session.CompleteLoadAsNew();

            return;
        }

        session.CompleteLoadMerged(current =>
        {
            if (current.ZClassId == PlayerPreferences.DefaultZombieClassId)
            {
                current.ZClassId = loaded.ZClassId;
            }

            if (current.HClassId == PlayerPreferences.DefaultHumanClassId)
            {
                current.HClassId = loaded.HClassId;
            }
        });

        core.Scheduler.NextWorldUpdate(() => { ApplyLoadedPreferences(steamId, session); });
    }

    private void ApplyLoadedPreferences(ulong steamId, PersistentSession<PlayerPreferences> session)
    {
        if (!sessions.IsCurrent(steamId, session))
        {
            return;
        }

        var player = core.PlayerManager
            .GetPlayerFromSteamId(steamId, false);

        if (player is not { IsValid: true, IsAuthorized: true, IsFakeClient: false })
        {
            return;
        }

        if (!player.IsAlive && playerManager.IsHuman(player))
        {
            playerManager.TrySetHuman(player);
        }
    }

    private async Task SaveAsync(ulong steamId, PersistentSession<PlayerPreferences> session, CancellationToken cancellationToken = default)
    {
        await session.SaveLock
            .WaitAsync(cancellationToken)
            .ConfigureAwait(false);

        try
        {
            var snapshot = session.CreateSnapshot(data => new PlayerPreferences
                {
                    ZClassId = data.ZClassId,
                    HClassId = data.HClassId,
                    KnifeId = data.KnifeId
                }
            );

            if (!snapshot.IsLoaded || !snapshot.IsDirty)
            {
                return;
            }

            await playerPersistenceService
                .SaveAsync(steamId, snapshot.Data)
                .ConfigureAwait(false);

            session.MarkSaved(snapshot.Revision);
        }
        finally
        {
            session.SaveLock.Release();
        }
    }
    
    private static bool CanInitialize(IPlayer player)
    {
        return player is
        {
            IsValid: true,
            IsAuthorized: true,
            IsFakeClient: false
        } && player.SteamID != 0;
    }

    private static bool CanSave(IPlayer player)
    {
        return player is
        {
            IsAuthorized: true,
            IsFakeClient: false
        } && player.SteamID != 0;
    }
}