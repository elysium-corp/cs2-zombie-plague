using System.Globalization;
using System.Text;
using System.Text.Json;
using Metrics.Core.Config;
using Metrics.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace Metrics.Core.Services;

internal sealed class MetricsSpool(
    ISwiftlyCore core,
    IOptions<MetricsConfig> config,
    ILogger<MetricsSpool> logger
) : IDisposable
{
    private const long MinimumCompactionPrefixBytes = 1_048_576;

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private readonly SemaphoreSlim _gate = new(1, 1);

    private string SpoolPath => Path.Combine(core.PluginDataDirectory, config.Value.SpoolFileName);

    private string CursorPath => SpoolPath + ".cursor";

    public async Task AppendAsync(
        IReadOnlyCollection<MetricEventEnvelope> events,
        CancellationToken cancellationToken
    )
    {
        if (!config.Value.PersistentSpoolEnabled || events.Count == 0)
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            Directory.CreateDirectory(core.PluginDataDirectory);
            await CompactAcknowledgedPrefixAsync(cancellationToken).ConfigureAwait(false);

            await using (var stream = new FileStream(
                             SpoolPath,
                             FileMode.Append,
                             FileAccess.Write,
                             FileShare.Read,
                             bufferSize: 16_384,
                             useAsync: true
                         ))
            await using (var writer = new StreamWriter(stream, Utf8NoBom))
            {
                foreach (var metricEvent in events)
                {
                    var line = JsonSerializer.Serialize(metricEvent, MetricsJson.Options);

                    await writer.WriteAsync(line.AsMemory(), cancellationToken).ConfigureAwait(false);
                    await writer.WriteAsync("\n".AsMemory(), cancellationToken).ConfigureAwait(false);
                }
            }

            await TrimToLimitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<SpoolBatch> ReadBatchAsync(int maxEvents, CancellationToken cancellationToken)
    {
        if (!config.Value.PersistentSpoolEnabled || !File.Exists(SpoolPath))
        {
            return SpoolBatch.Empty;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await using var stream = new FileStream(
                SpoolPath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                bufferSize: 16_384,
                useAsync: true
            );

            var startOffset = await ReadCursorAsync(cancellationToken).ConfigureAwait(false);

            if (startOffset < 0 || startOffset > stream.Length)
            {
                logger.LogWarning(
                    "Metrics spool cursor {SpoolCursor} is invalid for a {SpoolLength} byte file. The spool will be replayed from the beginning.",
                    startOffset,
                    stream.Length
                );

                startOffset = 0;
                await WriteCursorAsync(0, cancellationToken).ConfigureAwait(false);
            }

            stream.Position = startOffset;

            using var reader = new StreamReader(
                stream,
                Utf8NoBom,
                detectEncodingFromByteOrderMarks: false,
                bufferSize: 16_384,
                leaveOpen: true
            );

            var events = new List<MetricEventEnvelope>(Math.Min(maxEvents, 1_000));
            var nextOffset = startOffset;

            while (events.Count < maxEvents)
            {
                var line = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);

                if (line is null)
                {
                    break;
                }

                nextOffset += Utf8NoBom.GetByteCount(line) + 1L;

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                try
                {
                    var metricEvent = JsonSerializer.Deserialize<MetricEventEnvelope>(line, MetricsJson.Options);

                    if (metricEvent is not null)
                    {
                        events.Add(metricEvent);
                    }
                }
                catch (JsonException exception)
                {
                    logger.LogWarning(
                        exception,
                        "An invalid line in the Metrics spool will be skipped."
                    );
                }
            }

            return new SpoolBatch(events, startOffset, Math.Min(nextOffset, stream.Length));
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task AcknowledgeAsync(SpoolBatch batch, CancellationToken cancellationToken)
    {
        if (batch.NextOffset <= batch.StartOffset || !File.Exists(SpoolPath))
        {
            return;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            var spoolLength = new FileInfo(SpoolPath).Length;
            var nextOffset = Math.Min(batch.NextOffset, spoolLength);

            if (nextOffset >= spoolLength)
            {
                File.Delete(SpoolPath);
                DeleteIfExists(CursorPath);

                return;
            }

            await WriteCursorAsync(nextOffset, cancellationToken).ConfigureAwait(false);

            if (nextOffset >= MinimumCompactionPrefixBytes && nextOffset >= spoolLength / 2)
            {
                await CompactAcknowledgedPrefixAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private async Task CompactAcknowledgedPrefixAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(SpoolPath))
        {
            DeleteIfExists(CursorPath);

            return;
        }

        var cursor = await ReadCursorAsync(cancellationToken).ConfigureAwait(false);
        var spoolLength = new FileInfo(SpoolPath).Length;

        if (cursor <= 0)
        {
            return;
        }

        if (cursor >= spoolLength)
        {
            File.Delete(SpoolPath);
            DeleteIfExists(CursorPath);

            return;
        }

        var temporaryPath = SpoolPath + ".compact";

        try
        {
            await using (var source = new FileStream(
                             SpoolPath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read,
                             bufferSize: 16_384,
                             useAsync: true
                         ))
            await using (var destination = new FileStream(
                             temporaryPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 16_384,
                             useAsync: true
                         ))
            {
                source.Position = cursor;
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            // Предварительный сброс курсора после сбоя может привести только к повторной доставке.
            // Сервер устраняет дубликаты по eventId, поэтому это безопаснее, чем пропуск событий.
            await WriteCursorAsync(0, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, SpoolPath, overwrite: true);
        }
        finally
        {
            DeleteIfExists(temporaryPath);
        }
    }

    private async Task TrimToLimitAsync(CancellationToken cancellationToken)
    {
        var maxBytes = config.Value.MaxSpoolBytes;
        var file = new FileInfo(SpoolPath);

        if (!file.Exists || file.Length <= maxBytes)
        {
            return;
        }

        var bytesToDrop = file.Length - maxBytes;
        var retainedOffset = 0L;
        var droppedEvents = 0L;
        var boundaryFound = false;

        await using (var source = new FileStream(
                         SpoolPath,
                         FileMode.Open,
                         FileAccess.Read,
                         FileShare.Read,
                         bufferSize: 16_384,
                         useAsync: true
                     ))
        {
            var buffer = new byte[16_384];

            while (!boundaryFound)
            {
                var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                if (read == 0)
                {
                    retainedOffset = source.Length;
                    break;
                }

                for (var index = 0; index < read; index++)
                {
                    retainedOffset++;

                    if (buffer[index] != (byte)'\n')
                    {
                        continue;
                    }

                    droppedEvents++;

                    if (retainedOffset > bytesToDrop)
                    {
                        boundaryFound = true;
                        break;
                    }
                }
            }
        }

        if (retainedOffset >= file.Length)
        {
            File.Delete(SpoolPath);
            DeleteIfExists(CursorPath);
        }
        else
        {
            await CopyTailAndReplaceAsync(retainedOffset, cancellationToken).ConfigureAwait(false);
        }

        logger.LogWarning(
            "Metrics spool reached its {MaxSpoolBytes} byte limit. {DroppedEventCount} oldest event(s) were removed.",
            maxBytes,
            droppedEvents
        );
    }

    private async Task CopyTailAndReplaceAsync(long offset, CancellationToken cancellationToken)
    {
        var temporaryPath = SpoolPath + ".trim";

        try
        {
            await using (var source = new FileStream(
                             SpoolPath,
                             FileMode.Open,
                             FileAccess.Read,
                             FileShare.Read,
                             bufferSize: 16_384,
                             useAsync: true
                         ))
            await using (var destination = new FileStream(
                             temporaryPath,
                             FileMode.Create,
                             FileAccess.Write,
                             FileShare.None,
                             bufferSize: 16_384,
                             useAsync: true
                         ))
            {
                source.Position = offset;
                await source.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
                await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            await WriteCursorAsync(0, cancellationToken).ConfigureAwait(false);
            File.Move(temporaryPath, SpoolPath, overwrite: true);
        }
        finally
        {
            DeleteIfExists(temporaryPath);
        }
    }

    private async Task<long> ReadCursorAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(CursorPath))
        {
            return 0;
        }

        try
        {
            var value = await File.ReadAllTextAsync(CursorPath, cancellationToken).ConfigureAwait(false);

            return long.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var cursor)
                ? cursor
                : 0;
        }
        catch (IOException exception)
        {
            logger.LogWarning(exception, "Metrics spool cursor could not be read. The spool will be replayed safely.");

            return 0;
        }
    }

    private async Task WriteCursorAsync(long cursor, CancellationToken cancellationToken)
    {
        var temporaryPath = CursorPath + ".write";

        try
        {
            await File.WriteAllTextAsync(
                    temporaryPath,
                    cursor.ToString(CultureInfo.InvariantCulture),
                    Utf8NoBom,
                    cancellationToken
                )
                .ConfigureAwait(false);

            File.Move(temporaryPath, CursorPath, overwrite: true);
        }
        finally
        {
            DeleteIfExists(temporaryPath);
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}

internal sealed record SpoolBatch(
    IReadOnlyCollection<MetricEventEnvelope> Events,
    long StartOffset,
    long NextOffset
)
{
    public static readonly SpoolBatch Empty = new([], 0, 0);
}
