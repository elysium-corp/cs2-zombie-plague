namespace SupplyBox.Services;

internal enum SupplyBoxFallState { Falling, Landed, StartInSolid, NoSurface, InvalidTrace }

internal readonly record struct SupplyBoxFallHit(float Fraction, bool StartInSolid);
internal readonly record struct SupplyBoxFallStep(float Z, SupplyBoxFallState State);

// Расчёт движения отделён от движка, чтобы проверять столкновения и ограничения без CS2.
internal sealed class SupplyBoxFall(float targetZ, int dropHeight, int speed, long startedAt,
    Func<float, float, SupplyBoxFallHit> sweep)
{
    internal const float MinimumZ = -32768;
    private readonly long _startedAt = startedAt;
    private long _lastTick = startedAt;
    private readonly long _maximumFlightMilliseconds =
        (long)Math.Ceiling((targetZ + dropHeight - MinimumZ) / speed * 1000d) + 30000;

    public bool AutomaticLanding => targetZ == 0;
    public float SpawnZ => targetZ + dropHeight;

    public SupplyBoxFallStep Step(float currentZ, long now)
    {
        if (!float.IsFinite(currentZ)) return new(currentZ, SupplyBoxFallState.InvalidTrace);
        var delta = Math.Clamp((now - _lastTick) / 1000f, 0, 0.25f);
        _lastTick = now;
        if (!AutomaticLanding)
        {
            var z = Math.Max(targetZ, currentZ - speed * delta);
            return new(z, z <= targetZ ? SupplyBoxFallState.Landed : SupplyBoxFallState.Falling);
        }

        if (currentZ <= MinimumZ || now - _startedAt >= _maximumFlightMilliseconds)
            return new(currentZ, SupplyBoxFallState.NoSurface);
        if (delta == 0) return new(currentZ, SupplyBoxFallState.Falling);

        var nextZ = Math.Max(MinimumZ, currentZ - speed * delta);
        // Проверяем весь путь, поэтому высокая скорость не позволяет проскочить тонкий пол.
        var hit = sweep(currentZ, nextZ);
        if (hit.StartInSolid) return new(currentZ, SupplyBoxFallState.StartInSolid);
        if (!float.IsFinite(hit.Fraction) || hit.Fraction < 0 || hit.Fraction > 1)
            return new(currentZ, SupplyBoxFallState.InvalidTrace);
        if (hit.Fraction < 1)
            return new(currentZ + (nextZ - currentZ) * hit.Fraction, SupplyBoxFallState.Landed);
        return new(nextZ, nextZ <= MinimumZ ? SupplyBoxFallState.NoSurface : SupplyBoxFallState.Falling);
    }
}
