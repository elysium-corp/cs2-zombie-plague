using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Utils;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Natives;

namespace CustomEquipment.Services;

internal sealed class LaserMineSoundPlayback(
    LaserMineSettings settings,
    Action<string, Vector, float> emit,
    Func<long> clock)
{
    private long? _lastDamageSoundAt;

    public LaserMineSoundPlayback(ISwiftlyCore core, LaserMineSettings settings)
        : this(settings, (name, position, volume) => PlaySafely(core, name, position, volume),
            () => Environment.TickCount64)
    {
    }

    public void Charge(Vector position) => Play(settings.ChargeSound, position);
    public void Ready(Vector position) => Play(settings.ReadySound, position);
    public void Destroy(Vector position) => Play(settings.DestroySound, position);

    public void Damage(Vector position)
    {
        var now = clock();
        var interval = (long)MathF.Ceiling(settings.DamageSoundInterval * 1000f);
        if (_lastDamageSoundAt is { } last && now - last < interval) return;
        _lastDamageSoundAt = now;
        Play(settings.DamageSound, position);
    }

    public static void PlaySafely(ISwiftlyCore core, string name, Vector position, float volume)
    {
        try
        {
            SoundExt.PlayInPlace(name, position, volume);
        }
        catch (Exception exception)
        {
            core.Logger.LogWarning(exception, "[LaserMine] Не удалось воспроизвести звук {Sound}.", name);
        }
    }

    private void Play(string name, Vector position)
    {
        if (!string.IsNullOrWhiteSpace(name) && settings.SoundVolume > 0f)
        {
            emit(name, position, settings.SoundVolume);
        }
    }
}
