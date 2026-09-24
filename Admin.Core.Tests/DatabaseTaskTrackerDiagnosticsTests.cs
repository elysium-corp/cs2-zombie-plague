using Common.Database.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Admin.Core.Tests;

public sealed class DatabaseTaskTrackerDiagnosticsTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task RunAsync_PreservesCallingThreadAndOriginalPendingTask(bool diagnosticsEnabled)
    {
        var logger = new RecordingLogger();
        using var tracker = new DatabaseTaskTracker(logger, diagnosticsEnabled);
        var completion = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callingThread = Environment.CurrentManagedThreadId;
        var invocationThread = -1;

        var result = tracker.RunAsync(() =>
        {
            invocationThread = Environment.CurrentManagedThreadId;
            return completion.Task;
        }, "Load test account");

        try
        {
            Assert.Same(completion.Task, result);
            Assert.False(result.IsCompleted);
            Assert.Equal(callingThread, invocationThread);

            if (diagnosticsEnabled)
            {
                var entry = Assert.Single(logger.Diagnostics);
                Assert.Equal("Load test account", entry["Operation"]);
                Assert.Equal("WaitingForActivation", entry["TaskStatus"]);
                Assert.Equal(callingThread, entry["ThreadId"]);
                Assert.True(Assert.IsType<double>(entry["InvokeMs"]) >= 0);
                Assert.True(Assert.IsType<double>(entry["LockWaitMs"]) >= 0);
            }
            else
            {
                Assert.Empty(logger.Diagnostics);
            }
        }
        finally
        {
            completion.TrySetResult(42);
        }

        Assert.Equal(42, await result);
    }

    [Fact]
    public void Run_ReportsSynchronousDispatchWithoutWaitingForIo()
    {
        var logger = new RecordingLogger();
        using var tracker = new DatabaseTaskTracker(logger, diagnosticsEnabled: true);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        tracker.Run(() => completion.Task, "Load test preferences");

        try
        {
            Assert.False(completion.Task.IsCompleted);
            var entry = Assert.Single(logger.Diagnostics);
            Assert.Equal("Load test preferences", entry["Operation"]);
            Assert.Equal("WaitingForActivation", entry["TaskStatus"]);
        }
        finally
        {
            completion.TrySetResult();
        }
    }

    [Fact]
    public async Task RunAsync_SynchronousFailureKeepsOriginalException()
    {
        var logger = new RecordingLogger();
        using var tracker = new DatabaseTaskTracker(logger, diagnosticsEnabled: true);
        var failure = new InvalidOperationException("original operation failure");

        var task = tracker.RunAsync<int>(() => throw failure, "Load failing account");

        Assert.Same(failure, await Assert.ThrowsAsync<InvalidOperationException>(() => task));
        Assert.Equal("threw_synchronously", Assert.Single(logger.Diagnostics)["TaskStatus"]);
    }

    [Fact]
    public void Run_SynchronousFailureRemainsObservedInsteadOfEscaping()
    {
        var logger = new RecordingLogger();
        using var tracker = new DatabaseTaskTracker(logger, diagnosticsEnabled: true);
        var failure = new InvalidOperationException("original operation failure");

        var escaped = Record.Exception(() => tracker.Run(() => throw failure, "Load failing preferences"));

        Assert.Null(escaped);
        Assert.Contains(failure, logger.Failures);
        Assert.Equal("threw_synchronously", Assert.Single(logger.Diagnostics)["TaskStatus"]);
    }

    [Fact]
    public async Task RunAsync_DiagnosticLoggerFailureDoesNotChangeOperationResult()
    {
        var logger = new RecordingLogger { ThrowOnDiagnostics = true };
        using var tracker = new DatabaseTaskTracker(logger, diagnosticsEnabled: true);
        var original = Task.FromResult(42);

        var result = tracker.RunAsync(() => original, "Load test account");

        Assert.Same(original, result);
        Assert.Equal(42, await result);
    }

    private sealed class RecordingLogger : ILogger<DatabaseTaskTracker>
    {
        public List<Dictionary<string, object?>> Diagnostics { get; } = [];
        public List<Exception> Failures { get; } = [];
        public bool ThrowOnDiagnostics { get; init; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (exception is not null) Failures.Add(exception);
            if (!formatter(state, exception).StartsWith("[ConnectDiag]", StringComparison.Ordinal)) return;
            if (ThrowOnDiagnostics) throw new InvalidOperationException("diagnostic logger failure");
            Diagnostics.Add(((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary());
        }
    }
}
