using Common.Database.Sessions;
using Common.Database.Storages;
using Common.Database.Tasks;
using Common.Hooks.Abstractions;
using Economy.Api.Events;
using Economy.Core.Data.Store;
using Microsoft.Extensions.Logging;
using MSApi.Exceptions;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Economy.Core.Services;

internal sealed class PlayerAccountService(
    ISwiftlyCore core,
    IEconomyRulesProvider rulesProvider,
    EconomyPlayerRuleResolver playerRuleResolver,
    PlayerSessionStore<PlayerAccountState> sessions,
    IAccountPersistenceService persistenceService,
    DatabaseTaskTracker databaseTasks,
    SteamIdOperationQueue databaseOperations,
    IHookPublisher hooks
)
{
    public void Initialize(IPlayer player)
    {
        if (!CanInitialize(player))
        {
            return;
        }

        var steamId = player.SteamID;

        if (sessions.Get(steamId) is not null)
        {
            return;
        }

        var startBalance = Math.Min(
            rulesProvider.Current.StartMoney,
            playerRuleResolver.Resolve(player).MaxMoney
        );

        var session = sessions.Create(
            steamId,
            new PlayerAccountState
            {
                Balance = startBalance
            }
        );

        ApplyBalanceToGame(player, startBalance);

        var initializedContext = new EconomyAccountInitializedContext(player, startBalance);
        hooks.Dispatch(ref initializedContext);

        databaseTasks.Run(
            () => InitializeAsync(steamId, session, startBalance),
            $"Load economy account {steamId}"
        );
    }

    public void Remove(IPlayer player, bool save)
    {
        var steamId = player.SteamID;

        if (!sessions.TryRemove(steamId, out var session) || session is null)
        {
            return;
        }

        DispatchRemoved(steamId, session);

        if (save)
        {
            QueueSave(steamId, session);
        }
    }

    public void SaveAll()
    {
        foreach (var (steamId, session) in sessions.GetAll())
        {
            QueueSave(steamId, session);
        }
    }

    public void Shutdown(bool save)
    {
        var removedSessions = new List<KeyValuePair<ulong, PersistentSession<PlayerAccountState>>>();

        foreach (var (steamId, _) in sessions.GetAll())
        {
            if (!sessions.TryRemove(steamId, out var session) || session is null)
            {
                continue;
            }

            DispatchRemoved(steamId, session);
            removedSessions.Add(new KeyValuePair<ulong, PersistentSession<PlayerAccountState>>(
                steamId,
                session
            ));
        }

        if (save && removedSessions.Count > 0)
        {
            SaveBeforeShutdown(removedSessions);
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

    public void ReconcileAll()
    {
        foreach (var player in core.PlayerManager.GetAllValidPlayers())
        {
            if (player.IsFakeClient || !player.IsAuthorized)
            {
                continue;
            }

            ReconcileLimit(player);
        }
    }

    public void ReconcileLimit(IPlayer player)
    {
        var session = sessions.Get(player.SteamID);

        if (session is null)
        {
            return;
        }

        var snapshot = session.CreateSnapshot(data => data.Balance);

        if (!snapshot.IsLoaded)
        {
            return;
        }

        var maximum = playerRuleResolver.Resolve(player).MaxMoney;
        var balance = 0;
        var changed = session.TryUpdate(data =>
        {
            balance = Math.Clamp(data.Balance, 0, maximum);

            if (balance == data.Balance)
            {
                return false;
            }

            data.Balance = balance;
            return true;
        });

        if (!changed)
        {
            balance = session.Read(data => data.Balance);
        }

        ApplyBalanceToGame(player, balance);
    }

    private async Task InitializeAsync(
        ulong steamId,
        PersistentSession<PlayerAccountState> session,
        int startBalance,
        CancellationToken cancellationToken = default)
    {
        try
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

                        if (databaseBalance is null)
                        {
                            session.CompleteLoadAsNew();

                            return false;
                        }

                        var maxBalance = rulesProvider.Current.AbsoluteMaxMoney;

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

            var balance = session.Read(data => data.Balance);
            var loadedContext = new EconomyAccountLoadedContext(
                steamId,
                balance,
                isNew: !shouldRefreshProjection
            );

            hooks.Dispatch(ref loadedContext);

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
        catch (Exception exception)
        {
            var context = new EconomyAccountLoadFailedContext(steamId, exception);
            hooks.Dispatch(ref context);

            throw;
        }
    }

    private void ApplyLoadedBalance(ulong steamId, PersistentSession<PlayerAccountState> session)
    {
        if (!sessions.IsCurrent(steamId, session))
        {
            return;
        }

        var player = core.PlayerManager
            .GetPlayerFromSteamId(steamId, allowUnauthorized: false);

        if (player is not { IsValid: true, IsAuthorized: true, IsFakeClient: false })
        {
            return;
        }

        ReconcileLimit(player);
    }

    private async Task SaveAsync(
        ulong steamId,
        PersistentSession<PlayerAccountState> session,
        CancellationToken cancellationToken = default)
    {
        try
        {
            int? savedBalance = null;

            await databaseOperations.RunAsync(
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

                            savedBalance = snapshot.Data;
                        }
                        finally
                        {
                            session.SaveLock.Release();
                        }
                    }
                )
                .ConfigureAwait(false);

            if (savedBalance.HasValue)
            {
                var context = new EconomyAccountSavedContext(steamId, savedBalance.Value);
                hooks.Dispatch(ref context);
            }
        }
        catch (Exception exception)
        {
            var context = new EconomyAccountSaveFailedContext(steamId, exception);
            hooks.Dispatch(ref context);

            throw;
        }
    }

    private void QueueSave(ulong steamId, PersistentSession<PlayerAccountState> session)
    {
        databaseTasks.Run(
            cancellationToken => SaveAsync(steamId, session, cancellationToken),
            $"Save economy account {steamId}"
        );
    }

    private void SaveBeforeShutdown(
        IReadOnlyCollection<KeyValuePair<ulong, PersistentSession<PlayerAccountState>>> removedSessions)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var tasks = removedSessions
            .Select(pair => SaveAsync(pair.Key, pair.Value, timeout.Token))
            .ToArray();

        try
        {
            Task.WhenAll(tasks).GetAwaiter().GetResult();
        }
        catch (OperationCanceledException)
        {
            core.Logger.LogWarning(
                "Economy balance persistence exceeded the unload deadline."
            );
        }
        catch (Exception exception)
        {
            core.Logger.LogError(
                exception,
                "One or more economy balances could not be persisted during unload."
            );
        }
    }

    private void DispatchRemoved(ulong steamId, PersistentSession<PlayerAccountState> session)
    {
        var balance = session.Read(data => data.Balance);
        var context = new EconomyAccountRemovedContext(steamId, balance);
        hooks.Dispatch(ref context);
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
