using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;

namespace Economy.Core.Services;

internal sealed class EconomyRuntimeCoordinator(
    ISwiftlyCore core,
    IEconomyRulesProvider rulesProvider,
    PlayerAccountService playerAccountService,
    ILogger<EconomyRuntimeCoordinator> logger
) : IDisposable
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(3);
    private readonly Lock _lifecycleLock = new();
    private CancellationTokenSource? _shutdown;
    private Task? _worker;

    public void Start()
    {
        lock (_lifecycleLock)
        {
            if (_worker is not null)
            {
                return;
            }

            _shutdown = new CancellationTokenSource();
            _worker = Task.Run(() => RunAsync(_shutdown.Token));
        }
    }

    public void StopAndWait()
    {
        Task? worker;
        CancellationTokenSource? shutdown;

        lock (_lifecycleLock)
        {
            worker = _worker;
            shutdown = _shutdown;
            _worker = null;
            _shutdown = null;
        }

        if (worker is null || shutdown is null)
        {
            return;
        }

        shutdown.Cancel();

        try
        {
            if (!worker.Wait(ShutdownTimeout))
            {
                logger.LogWarning(
                    "Economy runtime coordinator exceeded the {TimeoutMs} ms shutdown deadline.",
                    ShutdownTimeout.TotalMilliseconds
                );
            }
        }
        catch (AggregateException exception) when (
            exception.InnerExceptions.All(inner => inner is OperationCanceledException))
        {
        }
        catch (AggregateException exception)
        {
            logger.LogError(
                exception.Flatten(),
                "Economy runtime coordinator stopped with an unexpected error."
            );
        }
        finally
        {
            shutdown.Dispose();
        }
    }

    public void Dispose()
    {
        StopAndWait();
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        var nextSettingsReload = DateTime.UtcNow + rulesProvider.Current.SettingsRefreshInterval;
        var nextPeriodicSave = NextPeriodicSave(DateTime.UtcNow, rulesProvider.Current);

        while (!cancellationToken.IsCancellationRequested)
        {
            var now = DateTime.UtcNow;
            var nextAction = nextPeriodicSave is null || nextSettingsReload <= nextPeriodicSave.Value
                ? nextSettingsReload
                : nextPeriodicSave.Value;
            var delay = nextAction - now;

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }

            now = DateTime.UtcNow;

            if (now >= nextSettingsReload)
            {
                var changed = await rulesProvider
                    .ReloadFromDatabaseAsync(cancellationToken)
                    .ConfigureAwait(false);

                var current = rulesProvider.Current;
                nextSettingsReload = now + current.SettingsRefreshInterval;

                if (changed)
                {
                    core.Scheduler.NextWorldUpdate(() =>
                    {
                        if (!cancellationToken.IsCancellationRequested)
                        {
                            playerAccountService.ReconcileAll();
                        }
                    });
                    nextPeriodicSave = NextPeriodicSave(now, current);
                }
            }

            var rules = rulesProvider.Current;

            if (nextPeriodicSave is not null && now >= nextPeriodicSave)
            {
                if (rules.Persistence.PeriodicSaveEnabled)
                {
                    playerAccountService.SaveAll();
                }

                nextPeriodicSave = NextPeriodicSave(now, rules);
            }
            else if (nextPeriodicSave is null && rules.Persistence.PeriodicSaveEnabled)
            {
                nextPeriodicSave = NextPeriodicSave(now, rules);
            }
        }
    }

    private static DateTime? NextPeriodicSave(DateTime now, Data.Rules.EconomyRulesSnapshot rules)
    {
        return rules.Persistence.PeriodicSaveEnabled
            ? now + rules.Persistence.PeriodicSaveInterval
            : null;
    }
}
