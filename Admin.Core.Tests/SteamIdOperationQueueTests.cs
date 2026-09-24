using System.Collections.Concurrent;
using Common.Database.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Admin.Core.Tests;

public sealed class SteamIdOperationQueueTests
{
    private static readonly TimeSpan TestTimeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task FirstOperationReturnsWhileSynchronousPrefixIsBlocked(bool withResult)
    {
        var queue = new SteamIdOperationQueue();
        using var release = new ManualResetEventSlim();
        var started = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callingThread = -1;

        void SynchronousPrefix()
        {
            started.TrySetResult(Environment.CurrentManagedThreadId);
            release.Wait();
        }

        Task Submit()
        {
            callingThread = Environment.CurrentManagedThreadId;
            if (withResult)
            {
                return queue.RunAsync(123, () =>
                {
                    SynchronousPrefix();
                    return Task.FromResult(42);
                });
            }

            return queue.RunAsync(123, () =>
            {
                SynchronousPrefix();
                return Task.CompletedTask;
            });
        }

        // Выделенный вызывающий поток не может быть повторно использован пулом.
        // Если префикс снова начнёт выполняться inline, возврат Submit будет заблокирован.
        var submission = Task.Factory.StartNew(Submit, CancellationToken.None,
            TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            var operation = await submission.WaitAsync(TestTimeout);
            Assert.NotEqual(callingThread, await started.Task.WaitAsync(TestTimeout));
            Assert.False(operation.IsCompleted);

            release.Set();
            await operation.WaitAsync(TestTimeout);
            if (withResult) Assert.Equal(42, await Assert.IsAssignableFrom<Task<int>>(operation));
        }
        finally
        {
            release.Set();
            await submission.Unwrap().WaitAsync(TestTimeout);
        }
    }

    [Fact]
    public async Task ReconnectReadsBalanceAfterEarlierLoadAndSave()
    {
        var queue = new SteamIdOperationQueue();
        var releaseLoad = NewSignal();
        var loadStarted = NewSignal();
        var order = new ConcurrentQueue<string>();
        var persistedBalance = 100;

        var load = queue.RunAsync(123, async () =>
        {
            order.Enqueue("load");
            loadStarted.TrySetResult();
            await releaseLoad.Task;
            return persistedBalance;
        });

        try
        {
            await loadStarted.Task.WaitAsync(TestTimeout);
            var save = queue.RunAsync(123, () =>
            {
                order.Enqueue("save");
                persistedBalance = 200;
                return Task.CompletedTask;
            });
            var reconnect = queue.RunAsync(123, () =>
            {
                order.Enqueue("reconnect");
                return Task.FromResult(persistedBalance);
            });

            Assert.False(save.IsCompleted);
            Assert.False(reconnect.IsCompleted);
            releaseLoad.TrySetResult();

            Assert.Equal(100, await load.WaitAsync(TestTimeout));
            await save.WaitAsync(TestTimeout);
            Assert.Equal(200, await reconnect.WaitAsync(TestTimeout));
            Assert.Equal(new[] { "load", "save", "reconnect" }, order.ToArray());
        }
        finally
        {
            releaseLoad.TrySetResult();
            await queue.RunAsync(123, () => Task.CompletedTask).WaitAsync(TestTimeout);
        }
    }

    [Fact]
    public async Task DifferentPlayerDoesNotWaitForAnotherPlayersIo()
    {
        var queue = new SteamIdOperationQueue();
        var release = NewSignal();
        var first = queue.RunAsync(123, () => release.Task);
        try
        {
            Assert.Equal(42, await queue.RunAsync(456, () => Task.FromResult(42)).WaitAsync(TestTimeout));
            Assert.False(first.IsCompleted);
        }
        finally
        {
            release.TrySetResult();
            await first.WaitAsync(TestTimeout);
        }
    }

    [Fact]
    public async Task SynchronousFailureDoesNotBlockFollowingOperation()
    {
        var queue = new SteamIdOperationQueue();
        var release = NewSignal();
        var predecessor = queue.RunAsync(123, () => release.Task);
        var failure = new InvalidOperationException("synchronous database failure");
        var failed = queue.RunAsync<int>(123, () => throw failure);
        var next = queue.RunAsync(123, () => Task.CompletedTask);

        release.TrySetResult();
        await predecessor.WaitAsync(TestTimeout);
        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(
            () => failed.WaitAsync(TestTimeout)));
        await next.WaitAsync(TestTimeout);
    }

    [Fact]
    public async Task AsynchronousFailureDoesNotBlockFollowingOperation()
    {
        var queue = new SteamIdOperationQueue();
        var completion = NewSignal();
        var failed = queue.RunAsync(123, () => completion.Task);
        var next = queue.RunAsync(123, () => Task.FromResult(42));
        var failure = new IOException("asynchronous database failure");

        completion.TrySetException(failure);
        Assert.Same(failure, await Assert.ThrowsAsync<IOException>(
            () => failed.WaitAsync(TestTimeout)));
        Assert.Equal(42, await next.WaitAsync(TestTimeout));
    }

    [Fact]
    public async Task CancelledOperationDoesNotBlockFollowingOperation()
    {
        var queue = new SteamIdOperationQueue();
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var cancelled = queue.RunAsync(123, () => completion.Task);
        var next = queue.RunAsync(123, () => Task.FromResult(42));

        completion.TrySetCanceled();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.WaitAsync(TestTimeout));
        Assert.True(cancelled.IsCanceled);
        Assert.Equal(42, await next.WaitAsync(TestTimeout));
    }

    [Fact]
    public async Task StopAndWaitDrainsQueuedSaveBeforeCancellingAcceptedOperations()
    {
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        var releaseLoad = NewSignal();
        var releaseSave = NewSignal();
        var saveStarted = NewSignal();
        var stoppingStarted = NewSignal();
        var saved = false;
        CancellationToken operationToken = default;

        tracker.Run(() => queue.RunAsync(123, () => releaseLoad.Task), "Load before shutdown");
        tracker.Run(token => queue.RunAsync(123, async () =>
        {
            operationToken = token;
            saveStarted.TrySetResult();
            await releaseSave.Task;
            token.ThrowIfCancellationRequested();
            saved = true;
        }), "Save before shutdown");

        var stopping = Task.Factory.StartNew(() =>
        {
            stoppingStarted.TrySetResult();
            tracker.StopAndWait();
        }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        try
        {
            await stoppingStarted.Task.WaitAsync(TestTimeout);
            releaseLoad.TrySetResult();
            await saveStarted.Task.WaitAsync(TestTimeout);
            Assert.False(stopping.IsCompleted);
            Assert.False(operationToken.IsCancellationRequested);

            releaseSave.TrySetResult();
            await stopping.WaitAsync(TestTimeout);
            Assert.True(saved);
            Assert.True(operationToken.IsCancellationRequested);

            var rejectedInvoked = false;
            tracker.Run(() => { rejectedInvoked = true; return Task.CompletedTask; });
            Assert.False(rejectedInvoked);
            await Assert.ThrowsAsync<InvalidOperationException>(() => tracker.RunAsync(() => Task.FromResult(42)));
        }
        finally
        {
            releaseLoad.TrySetResult();
            releaseSave.TrySetResult();
            await stopping.WaitAsync(TestTimeout);
        }
    }

    [Fact]
    public async Task StopAndWaitCancelsStalledIoAndWaitsForItsCleanup()
    {
        var queue = new SteamIdOperationQueue();
        using var tracker = new DatabaseTaskTracker(NullLogger<DatabaseTaskTracker>.Instance);
        var neverCompletes = NewSignal();
        var started = NewSignal();
        var cleanedUp = false;

        tracker.Run(token => queue.RunAsync(123, async () =>
        {
            try
            {
                started.TrySetResult();
                await neverCompletes.Task.WaitAsync(token);
            }
            finally
            {
                cleanedUp = true;
            }
        }), "Stalled database operation");

        await started.Task.WaitAsync(TestTimeout);
        await Task.Run(tracker.StopAndWait).WaitAsync(TestTimeout);
        Assert.True(cleanedUp);
    }

    private static TaskCompletionSource NewSignal() => new(TaskCreationOptions.RunContinuationsAsynchronously);
}
