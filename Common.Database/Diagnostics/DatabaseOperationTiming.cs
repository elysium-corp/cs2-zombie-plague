using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Common.Database.Diagnostics;

/// <summary>
/// Измеряет синхронный запуск операции отдельно от ожидания блокировки трекера.
/// Вывод выполняется после выхода из блокировки; продолжение Task не изменяется.
/// </summary>
internal sealed class DatabaseOperationTiming(ILogger logger, string? operationName) : IDisposable
{
    private readonly long _started = Stopwatch.GetTimestamp();
    private readonly int _threadId = Environment.CurrentManagedThreadId;
    private long? _lockAcquired;
    private long? _invocationStarted;
    private long? _invocationFinished;
    private string _status = "not_invoked";

    internal void LockAcquired() => _lockAcquired = Stopwatch.GetTimestamp();

    internal void InvocationStarted() => _invocationStarted = Stopwatch.GetTimestamp();

    internal void InvocationReturned(Task task)
    {
        _invocationFinished = Stopwatch.GetTimestamp();
        _status = task.Status.ToString();
    }

    internal void InvocationFailed()
    {
        _invocationFinished = Stopwatch.GetTimestamp();
        _status = "threw_synchronously";
    }

    public void Dispose()
    {
        var finished = Stopwatch.GetTimestamp();
        var lockWait = _lockAcquired.HasValue
            ? Stopwatch.GetElapsedTime(_started, _lockAcquired.Value).TotalMilliseconds
            : 0;
        var invocation = _invocationStarted.HasValue && _invocationFinished.HasValue
            ? Stopwatch.GetElapsedTime(_invocationStarted.Value, _invocationFinished.Value).TotalMilliseconds
            : 0;

        try
        {
            logger.LogInformation(
                "[ConnectDiag] stage=db_dispatch operation={Operation} start_tick={StartTick} " +
                "lock_wait_ms={LockWaitMs:F3} invoke_ms={InvokeMs:F3} dispatch_ms={DispatchMs:F3} " +
                "task_status_at_return={TaskStatus} thread={ThreadId}",
                operationName, _started, lockWait, invocation,
                Stopwatch.GetElapsedTime(_started, finished).TotalMilliseconds, _status, _threadId);
        }
        catch
        {
            // Диагностика не должна менять результат операции или обработку её исключений.
        }
    }
}
