using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Utils;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Services;

internal sealed class LaserMineSoundPlayback(
    LaserMineSettings settings,
    Action<string, Vector, float> emit,
    Func<long> clock,
    Action<string, Vector, float>? emitDestruction = null)
{
    private long? _lastDamageSoundAt;

    public LaserMineSoundPlayback(ISwiftlyCore core, LaserMineSettings settings, Func<CBaseEntity?> source)
        : this(settings, (name, position, volume) => PlaySafely(core, name, position, volume, source()),
            () => Environment.TickCount64,
            (name, position, volume) => PlaySafely(core, name, position, volume))
    {
    }

    public void Start(Vector position, Action<float, Action> schedule)
    {
        Play(settings.InstallSound, position);
        schedule(settings.InstallSoundDuration, () => Charge(position));
        schedule(settings.ReadySoundDelay, () => Ready(position));
    }

    public void Charge(Vector position) => Play(settings.ChargeSound, position);
    public void Ready(Vector position) => Play(settings.ReadySound, position);
    public void Destroy(Vector position) => Play(settings.DestroySound, position, emitDestruction);

    public void Damage(Vector position)
    {
        var now = clock();
        var interval = (long)MathF.Ceiling(settings.DamageSoundInterval * 1000f);
        if (_lastDamageSoundAt is { } last && now - last < interval) return;
        _lastDamageSoundAt = now;
        Play(settings.DamageSound, position);
    }

    private static void PlaySafely(ISwiftlyCore core, string name, Vector position, float volume, CBaseEntity? source = null)
    {
        try
        {
            if (source is { IsValidEntity: true })
            {
                // block_match_entity в soundevents различает отдельные установленные мины.
                SoundExt.PlayInPlace(source, name, position, volume);
            }
            else
            {
                SoundExt.PlayInPlace(name, position, volume);
            }
        }
        catch (Exception exception)
        {
            core.Logger.LogWarning(exception, "[LaserMine] Не удалось воспроизвести звук {Sound}.", name);
        }
    }

    private void Play(string name, Vector position, Action<string, Vector, float>? emitter = null)
    {
        if (!string.IsNullOrWhiteSpace(name) && settings.SoundVolume > 0f)
        {
            (emitter ?? emit)(name, position, settings.SoundVolume);
        }
    }
}
