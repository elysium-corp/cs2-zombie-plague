using Microsoft.Extensions.Logging;

namespace Common.Database.Tasks;

public sealed class DatabaseTaskTracker(ILogger<DatabaseTaskTracker> logger) : IDisposable
{
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(3);
    private readonly Lock _lock = new();

    private readonly HashSet<Task> _tasks = [];
    private readonly CancellationTokenSource _shutdown = new();

    private bool _stopping;

    public void Run(Func<Task> operation, string? operationName = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_lock)
        {
            if (_stopping)
            {
                logger.LogWarning("Database operation '{OperationName}' was ignored because the tracker is stopping!", operationName);

                return;
            }

            Task operationTask;

            try
            {
                operationTask = operation();
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to start database operation '{OperationName}'!",
                    operationName
                );

                return;
            }

            var observedTask = ObserveAsync(operationTask, operationName);

            _tasks.Add(observedTask);

            _ = observedTask.ContinueWith(
                Remove,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
        }
    }

    public void Run(Func<CancellationToken, Task> operation, string? operationName = null) =>
        Run(() => operation(_shutdown.Token), operationName);
    
    public Task<TResult> RunAsync<TResult>(Func<Task<TResult>> operation, string? operationName = null)
    {
        ArgumentNullException.ThrowIfNull(operation);

        lock (_lock)
        {
            if (_stopping)
            {
                logger.LogWarning(
                    "Database operation '{OperationName}' was ignored because the tracker is stopping!",
                    operationName
                );

                return Task.FromException<TResult>(
                    new InvalidOperationException("Database task tracker is stopping!")
                );
            }

            Task<TResult> operationTask;

            try
            {
                operationTask = operation();
            }
            catch (Exception exception)
            {
                logger.LogError(
                    exception,
                    "Failed to start database operation '{OperationName}'!",
                    operationName
                );

                return Task.FromException<TResult>(exception);
            }

            var observedTask = ObserveAsync(operationTask, operationName);

            _tasks.Add(observedTask);

            _ = observedTask.ContinueWith(
                Remove,
                CancellationToken.None,
                TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );

            return operationTask;
        }
    }

    public Task<TResult> RunAsync<TResult>(Func<CancellationToken, Task<TResult>> operation, string? operationName = null) =>
        RunAsync(() => operation(_shutdown.Token), operationName);

    public void StopAndWait()
    {
        Task[] pendingTasks;

        lock (_lock)
        {
            _stopping = true;
            _shutdown.Cancel();

            pendingTasks = _tasks.ToArray();
        }

        if (pendingTasks.Length == 0)
        {
            return;
        }

        if (!Task.WhenAll(pendingTasks).Wait(ShutdownTimeout))
            logger.LogWarning("{Count} database operation(s) exceeded the {TimeoutMs} ms shutdown deadline.", pendingTasks.Length, ShutdownTimeout.TotalMilliseconds);
    }

    public void Dispose()
    {
        StopAndWait();
        _shutdown.Dispose();
    }

    private async Task ObserveAsync(Task task, string? operationName)
    {
        try
        {
            await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            logger.LogDebug(
                "Database operation '{OperationName}' was cancelled!",
                operationName
            );
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Database operation '{OperationName}' failed!",
                operationName
            );
        }
    }

    private void Remove(Task task)
    {
        lock (_lock)
        {
            _tasks.Remove(task);
        }
    }
}
