namespace Common.Database.Tasks;

public sealed class SteamIdOperationQueue
{
    private readonly Lock _lock = new();

    private readonly Dictionary<ulong, Task> _tails = [];

    public Task RunAsync(ulong steamId, Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        Task previous;
        var completion = new TaskCompletionSource<object?>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_lock)
        {
            previous = _tails.GetValueOrDefault(steamId, Task.CompletedTask);

            _tails[steamId] = completion.Task;
        }

        _ = ExecuteAsync(
            steamId,
            previous,
            operation,
            completion
        );

        return completion.Task;
    }

    public Task<TResult> RunAsync<TResult>(ulong steamId, Func<Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        Task previous;
        var completion = new TaskCompletionSource<TResult>(TaskCreationOptions.RunContinuationsAsynchronously);

        lock (_lock)
        {
            previous = _tails.GetValueOrDefault(steamId, Task.CompletedTask);

            _tails[steamId] = completion.Task;
        }

        _ = ExecuteAsync(
            steamId,
            previous,
            operation,
            completion
        );

        return completion.Task;
    }

    private async Task ExecuteAsync(
        ulong steamId,
        Task previous,
        Func<Task> operation,
        TaskCompletionSource<object?> completion)
    {
        try
        {
            await WaitPreviousAsync(previous)
                .ConfigureAwait(false);

            await operation()
                .ConfigureAwait(false);

            completion.TrySetResult(null);
        }
        catch (OperationCanceledException)
        {
            completion.TrySetCanceled();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            RemoveIfTail(steamId, completion.Task);
        }
    }

    private async Task ExecuteAsync<TResult>(
        ulong steamId,
        Task previous,
        Func<Task<TResult>> operation,
        TaskCompletionSource<TResult> completion
    )
    {
        try
        {
            await WaitPreviousAsync(previous)
                .ConfigureAwait(false);

            var result = await operation()
                .ConfigureAwait(false);

            completion.TrySetResult(result);
        }
        catch (OperationCanceledException)
        {
            completion.TrySetCanceled();
        }
        catch (Exception exception)
        {
            completion.TrySetException(exception);
        }
        finally
        {
            RemoveIfTail(steamId, completion.Task);
        }
    }

    private static async Task WaitPreviousAsync(Task previous)
    {
        try
        {
            await previous.ConfigureAwait(false);
        }
        catch
        {
            // Ошибка предыдущей операции уже будет
            // обработана её DatabaseTaskTracker.
            //
            // Следующая операция всё равно должна
            // продолжить выполнение.
        }
    }

    private void RemoveIfTail(ulong steamId, Task completedTask)
    {
        lock (_lock)
        {
            if (_tails.TryGetValue(steamId, out var tail) && ReferenceEquals(tail, completedTask))
            {
                _tails.Remove(steamId);
            }
        }
    }
}