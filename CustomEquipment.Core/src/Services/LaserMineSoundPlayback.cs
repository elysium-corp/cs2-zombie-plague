using CustomEquipment.Data.GameplayItems;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Services;

internal sealed class LaserMineSoundPlayback(
    LaserMineSettings settings,
    Action<string, Vector, float, float> emit,
    Func<long> clock,
    Action<string, Vector, float, float>? emitDestruction = null,
    Action? stop = null)
{
    private readonly Action<string, Vector, float, float> _emit = emit;
    private readonly Action? _stop = stop;
    private long? _lastDamageSoundAt;
    private bool _destroyed;
    private bool _stopped;

    public LaserMineSoundPlayback(LaserMineSettings settings, LaserMineSoundService sounds, Func<CBaseEntity?> source)
        : this(settings, (_, _, _, _) => { }, () => Environment.TickCount64,
            (name, position, volume, duration) => sounds.Play(name, position, volume, -1, duration))
    {
        var sourceIndex = -1;
        _emit = (name, position, volume, duration) =>
        {
            if (source() is not { IsValidEntity: true } entity) return;
            sourceIndex = (int)entity.Index;
            sounds.Play(name, position, volume, sourceIndex, duration);
        };
        _stop = () =>
        {
            if (sourceIndex != -1) sounds.StopSource(sourceIndex);
        };
    }

    public void Start(Vector position, Action<float, Action> schedule)
    {
        if (_destroyed || _stopped) return;
        Play(settings.InstallSound, position, settings.InstallSoundDuration);
        schedule(settings.InstallSoundDuration, () => Charge(position));
        schedule(settings.ReadySoundDelay, () => Ready(position));
    }

    public void Charge(Vector position) => Play(settings.ChargeSound, position, settings.ChargeSoundDuration);
    public void Ready(Vector position) => Play(settings.ReadySound, position, settings.ReadySoundDuration);

    public void Destroy(Vector position)
    {
        if (_destroyed || _stopped) return;
        _destroyed = true;
        Emit(settings.DestroySound, position, settings.DestroySoundDuration, emitDestruction ?? _emit);
    }

    public void Stop()
    {
        if (_stopped) return;
        _stopped = true;
        _stop?.Invoke();
    }

    public void Damage(Vector position)
    {
        if (_destroyed || _stopped) return;
        var now = clock();
        var interval = (long)MathF.Ceiling(settings.DamageSoundInterval * 1000f);
        if (_lastDamageSoundAt is { } last && now - last < interval) return;
        _lastDamageSoundAt = now;
        Play(settings.DamageSound, position, settings.DamageSoundInterval);
    }

    private void Play(string name, Vector position, float duration)
    {
        if (!_destroyed && !_stopped) Emit(name, position, duration, _emit);
    }

    private void Emit(string name, Vector position, float duration, Action<string, Vector, float, float> emitter)
    {
        if (!string.IsNullOrWhiteSpace(name) && settings.SoundVolume > 0f && duration > 0f)
        {
            emitter(name, position, settings.SoundVolume, duration);
        }
    }
}
