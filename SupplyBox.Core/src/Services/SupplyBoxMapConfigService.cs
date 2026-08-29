using System.Text.Json;
using Microsoft.Extensions.Logging;
using SupplyBox.Data.Configs;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;

namespace SupplyBox.Services;

internal sealed class SupplyBoxMapConfigService(ISwiftlyCore core) : IDisposable
{
    internal const int MaximumConfigBytes = 1_048_576;
    internal const int MaximumPoints = 512;
    private readonly JsonSerializerOptions _json = new() { WriteIndented = true, PropertyNameCaseInsensitive = true, IncludeFields = true };
    private readonly Lock _state = new();
    private readonly SemaphoreSlim _io = new(1, 1);
    private readonly CancellationTokenSource _shutdown = new();
    private readonly HashSet<Task> _tasks = [];
    private List<SupplyBoxEntityConfig> _points = [];
    private string? _path;
    private long _generation;
    private long _version;
    private int _disposed;

    public IReadOnlyList<SupplyBoxEntityConfig> GetSnapshot()
    {
        lock (_state) return _points.Select(Clone).ToArray();
    }

    public void LoadConfig(string mapName)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        var safeName = new string(mapName.Where(character => char.IsLetterOrDigit(character) || character is '_' or '-').Take(128).ToArray());
        if (safeName.Length == 0) throw new ArgumentException("Invalid map name.", nameof(mapName));
        var path = Path.Combine(core.PluginPath, "SupplyBox", safeName + ".json");
        var generation = Interlocked.Increment(ref _generation);
        lock (_state) { _path = path; _points = []; _version = 0; }
        Track(Task.Run(() => LoadAsync(path, generation, _shutdown.Token)));
    }

    public bool TryAdd(Vector position, Vector rotation)
    {
        List<SupplyBoxEntityConfig> snapshot;
        string path;
        long generation;
        long version;
        lock (_state)
        {
            if (_path is null || _points.Count >= MaximumPoints) return false;
            var used = _points.Select(point => point.Index).ToHashSet();
            var index = Enumerable.Range(1, MaximumPoints).First(value => !used.Contains(value));
            _points.Add(new SupplyBoxEntityConfig { Index = index, Position = position, Rotation = rotation });
            snapshot = _points.Select(Clone).ToList();
            path = _path; generation = _generation; version = ++_version;
        }
        QueueSave(path, snapshot, generation, version);
        return true;
    }

    public bool TryRemove(int index)
    {
        List<SupplyBoxEntityConfig> snapshot;
        string path;
        long generation;
        long version;
        lock (_state)
        {
            if (_path is null || _points.RemoveAll(point => point.Index == index) == 0) return false;
            snapshot = _points.Select(Clone).ToList();
            path = _path; generation = _generation; version = ++_version;
        }
        QueueSave(path, snapshot, generation, version);
        return true;
    }

    private async Task LoadAsync(string path, long generation, CancellationToken token)
    {
        try
        {
            await _io.WaitAsync(token).ConfigureAwait(false);
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                MapSupplyBoxEntityConfig config;
                if (!File.Exists(path)) config = new();
                else
                {
                    if (new FileInfo(path).Length > MaximumConfigBytes) throw new InvalidDataException("SupplyBox map config exceeds 1 MiB.");
                    await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 16_384, true);
                    config = await JsonSerializer.DeserializeAsync<MapSupplyBoxEntityConfig>(stream, _json, token).ConfigureAwait(false) ?? new();
                }
                config.SupplyBoxes ??= [];
                if (config.SupplyBoxes.Count > MaximumPoints || config.SupplyBoxes.Any(point => point.Index <= 0) || config.SupplyBoxes.Select(point => point.Index).Distinct().Count() != config.SupplyBoxes.Count)
                    throw new InvalidDataException("SupplyBox map config contains invalid or duplicate points.");
                lock (_state) if (generation == _generation) _points = config.SupplyBoxes.Select(Clone).ToList();
                if (!File.Exists(path)) QueueSave(path, [], generation, 0);
            }
            finally { _io.Release(); }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) core.Logger.LogError(exception, "Failed to load SupplyBox config {Path}.", path); }
    }

    private void QueueSave(string path, List<SupplyBoxEntityConfig> points, long generation, long version) =>
        Track(Task.Run(() => SaveAsync(path, points, generation, version, _shutdown.Token)));

    private async Task SaveAsync(string path, List<SupplyBoxEntityConfig> points, long generation, long version, CancellationToken token)
    {
        try
        {
            await _io.WaitAsync(token).ConfigureAwait(false);
            try
            {
                lock (_state) if (generation != _generation || version != _version) return;
                var temporary = path + ".tmp";
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                    await using (var stream = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 16_384, FileOptions.Asynchronous | FileOptions.WriteThrough))
                        await JsonSerializer.SerializeAsync(stream, new MapSupplyBoxEntityConfig { SupplyBoxes = points }, _json, token).ConfigureAwait(false);
                    token.ThrowIfCancellationRequested();
                    File.Move(temporary, path, true);
                }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
            }
            finally { _io.Release(); }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        catch (Exception exception) { if (!token.IsCancellationRequested) core.Logger.LogError(exception, "Failed to save SupplyBox config {Path}.", path); }
    }

    private void Track(Task task)
    {
        lock (_tasks) _tasks.Add(task);
        _ = task.ContinueWith(completed => { lock (_tasks) _tasks.Remove(completed); }, TaskScheduler.Default);
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        _shutdown.Cancel();
        Task[] tasks; lock (_tasks) tasks = _tasks.ToArray();
        var completed = Task.WhenAll(tasks).Wait(TimeSpan.FromSeconds(2));
        _shutdown.Dispose();
        if (completed) _io.Dispose();
    }

    private static SupplyBoxEntityConfig Clone(SupplyBoxEntityConfig point) => new() { Index = point.Index, Position = point.Position, Rotation = point.Rotation };
}
