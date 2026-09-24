using Common.Di.Diagnostics;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Common.Di.Tests;

public sealed class ConnectionDiagnosticsTests
{
    [Fact]
    public void Timing_RecordsOneExitWithSessionAndRetryIdentity()
    {
        var logger = new RecordingLogger();
        var timing = new ConnectionDiagnostics.Timing(logger, "ZombiePlague.player_ready", 3, 123, 2);

        timing.Dispose();
        timing.Dispose();

        var entry = Assert.Single(logger.Entries);
        Assert.Equal("ZombiePlague.player_ready", entry["Stage"]);
        Assert.Equal(3, entry["PlayerId"]);
        Assert.Equal(123UL, entry["SessionId"]);
        Assert.Equal(2, entry["Attempt"]);
    }

    [Fact]
    public void Timing_LoggerFailureCannotReplaceHandlerException()
    {
        var logger = new RecordingLogger { ThrowOnLog = true };
        var failure = new InvalidOperationException("original handler failure");

        var escaped = Assert.Throws<InvalidOperationException>(() =>
        {
            using var timing = new ConnectionDiagnostics.Timing(logger, "test", 1, 0, -1);
            throw failure;
        });

        Assert.Same(failure, escaped);
    }

    private sealed class RecordingLogger : ILogger
    {
        public List<Dictionary<string, object?>> Entries { get; } = [];
        public bool ThrowOnLog { get; init; }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (ThrowOnLog) throw new InvalidOperationException("diagnostic logger failure");
            Entries.Add(((IEnumerable<KeyValuePair<string, object?>>)state!).ToDictionary());
        }
    }
}
