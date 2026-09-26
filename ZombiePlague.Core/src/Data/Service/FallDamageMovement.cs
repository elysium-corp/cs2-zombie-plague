using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace ZombiePlague.Core.Data.Service;

// Правила, по которым урон от падения не меняет горизонтальное движение игрока.
internal static class FallDamageMovement
{
    private const float VelocityTolerance = 0.5f;

    public static bool IsFallDamage(DamageTypes_t damageType)
    {
        return (damageType & DamageTypes_t.DMG_FALL) != 0;
    }

    // Возвращает скорость с исходной горизонтальной составляющей, если обработка
    // урона её изменила. Вертикальная составляющая остаётся той, что задал движок.
    public static bool TryRestore(Vector before, Vector after, out Vector velocity)
    {
        if (MathF.Abs(after.X - before.X) <= VelocityTolerance &&
            MathF.Abs(after.Y - before.Y) <= VelocityTolerance)
        {
            velocity = after;
            return false;
        }

        velocity = new Vector(before.X, before.Y, after.Z);
        return true;
    }
}
