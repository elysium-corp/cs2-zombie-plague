using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.ProtobufDefinitions;
using SwiftlyS2.Shared.Sounds;

namespace CustomEquipment.Services;

internal sealed class LaserMineSoundService(
    Func<string, Vector, float, int, uint> emit,
    Action<uint> stop,
    Func<float, Action, CancellationTokenSource> schedule) : IDisposable
{
    private readonly Dictionary<uint, (int Source, CancellationTokenSource Timer)> _active = [];
    private bool _disposed;

    public LaserMineSoundService(ISwiftlyCore core)
        : this((name, position, volume, source) => EmitSafely(core, name, position, volume, source),
            id => StopSafely(core, id), core.Scheduler.DelayBySeconds)
    {
    }

    public void Play(string name, Vector position, float volume, int source, float duration)
    {
        if (_disposed || !float.IsFinite(duration) || duration <= 0f) return;
        var id = emit(name, position, volume, source);
        if (id == 0) return;
        try
        {
            _active.Add(id, (source, schedule(duration, () => Stop(id))));
        }
        catch
        {
            stop(id);
            throw;
        }
    }

    public void StopSource(int source)
    {
        foreach (var id in _active.Where(pair => pair.Value.Source == source).Select(pair => pair.Key).ToArray())
        {
            Stop(id);
        }
    }

    public void StopAll()
    {
        foreach (var id in _active.Keys.ToArray()) Stop(id);
    }

    public void Dispose()
    {
        _disposed = true;
        StopAll();
    }

    private void Stop(uint id)
    {
        if (!_active.Remove(id, out var sound)) return;
        try
        {
            sound.Timer.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Планировщик мог освободить таймер при смене карты.
        }

        stop(id);
    }

    private static uint EmitSafely(ISwiftlyCore core, string name, Vector position, float volume, int source)
    {
        try
        {
            using var sound = new SoundEvent(name) { Volume = volume, SourceEntityIndex = source };
            sound.SetFloat3("public.position", position.X, position.Y, position.Z);
            sound.Recipients.AddAllPlayers();
            // Dispose освобождает оболочку, но не останавливает событие на клиентах.
            return sound.Emit();
        }
        catch (Exception exception)
        {
            core.Logger.LogWarning(exception, "[LaserMine] Не удалось воспроизвести звук {Sound}.", name);
            return 0;
        }
    }

    private static void StopSafely(ISwiftlyCore core, uint id)
    {
        try
        {
            using var message = core.NetMessage.Create<CMsgSosStopSoundEvent>();
            message.SoundeventGuid = unchecked((int)id);
            message.SendToAllPlayers();
        }
        catch (Exception exception)
        {
            core.Logger.LogWarning(exception, "[LaserMine] Не удалось остановить звук {SoundId}.", id);
        }
    }
}
