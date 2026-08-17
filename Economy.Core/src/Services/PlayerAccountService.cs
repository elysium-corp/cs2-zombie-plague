using Common.Database.Sessions;
using Common.Database.Storages;
using Common.Database.Tasks;
using Economy.Core.Data.Configs;
using Economy.Core.Data.Store;
using Microsoft.Extensions.Options;
using MSApi.Exceptions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Economy.Core.Services;

internal sealed class PlayerAccountService(
    ISwiftlyCore core,
    IOptions<EconomyConfig> config,
    PlayerSessionStore<PlayerAccountState> sessions,
    IAccountPersistenceService persistenceService,
    DatabaseTaskTracker databaseTasks,
    SteamIdOperationQueue databaseOperations
)
{
    public void Initialize(IPlayer player)
    {
        if (!CanInitialize(player))
        {
            return;
        }

        var steamId = player.SteamID;
        var startBalance = config.Value.StartMoney;

        var session = sessions.Create(
            steamId,
            new PlayerAccountState
            {
                Balance = startBalance
            }
        );

        ApplyBalanceToGame(player, startBalance);

        databaseTasks.Run(
            () => InitializeAsync(steamId, session, startBalance),
            $"Load economy account {steamId}"
        );
    }

    public void Remove(IPlayer player)
    {
        var steamId = player.SteamID;

        if (!sessions.TryRemove(steamId, out var session) || session is null)
        {
            return;
        }

        databaseTasks.Run(
            () => SaveAsync(steamId, session),
            $"Save economy account {steamId}"
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

            databaseTasks.Run(
                () => SaveAsync(steamId, session),
                $"Save economy account {steamId}"
            );
        }

        databaseTasks.StopAndWait();
    }

    public void RefreshProjection(IPlayer player)
    {
        var session = sessions.Get(player.SteamID);

        if (session is null)
        {
            return;
        }

        var balance = session.Read(data => data.Balance);

        ApplyBalanceToGame(player, balance);
    }

    private async Task InitializeAsync(
        ulong steamId,
        PersistentSession<PlayerAccountState> session,
        int startBalance,
        CancellationToken cancellationToken = default)
    {
        var shouldRefreshProjection =
            await databaseOperations.RunAsync(
                steamId,
                async () =>
                {
                    var databaseBalance =
                        await persistenceService
                            .LoadAsync(steamId, cancellationToken)
                            .ConfigureAwait(false);

                    if (!sessions.IsCurrent(steamId, session))
                    {
                        return false;
                    }

                    if (databaseBalance is null)
                    {
                        session.CompleteLoadAsNew();

                        return false;
                    }

                    var maxBalance = config.Value.MaxMoney;

                    session.CompleteLoadMerged(
                        current =>
                        {
                            var localDelta = (long)current.Balance - startBalance;

                            var mergedBalance = databaseBalance.Value + localDelta;

                            current.Balance = (int)Math.Clamp(mergedBalance, 0L, maxBalance);
                        }
                    );

                    return true;
                }
            )
            .ConfigureAwait(false);

        if (!shouldRefreshProjection)
        {
            return;
        }

        core.Scheduler.NextWorldUpdate(
            () =>
            {
                ApplyLoadedBalance(steamId, session);
            }
        );
    }

    private void ApplyLoadedBalance(ulong steamId, PersistentSession<PlayerAccountState> session)
    {
        if (!sessions.IsCurrent(steamId))
        {
            return;
        }

        var player = core.PlayerManager
            .GetPlayerFromSteamId(steamId, allowUnauthorized: false);

        if (player is not { IsValid: true, IsAuthorized: true, IsFakeClient: false })
        {
            return;
        }

        var balance = session.Read(data => data.Balance);

        ApplyBalanceToGame(player, balance);
    }

    private Task SaveAsync(
        ulong steamId,
        PersistentSession<PlayerAccountState> session,
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
                    var snapshot = session.CreateSnapshot(data => data.Balance);

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

    private static void ApplyBalanceToGame(IPlayer player, int balance)
    {
        var moneyServices = player.Controller.InGameMoneyServices
                            ?? throw new MoneyServicesNotFoundException("Player money services were not found!");

        moneyServices.Account = balance;
        moneyServices.AccountUpdated();
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
}