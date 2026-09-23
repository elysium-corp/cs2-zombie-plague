using CustomEquipment.Api.Data.Models;
using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Utils;

internal static class WeaponHandlingRuntime
{
    internal static (bool NoRecoil, bool NoSpread) GetOverrides(
        WeaponRecoil? recoil, WeaponAccuracy? accuracy, int mode)
    {
        var noRecoil = recoil is not null
                       && IsZero(recoil.Magnitude, mode)
                       && IsZero(recoil.MagnitudeVariance, mode);
        var noSpread = accuracy is not null
                       && IsZero(accuracy.Spread, mode)
                       && IsZero(accuracy.InaccuracyStand, mode)
                       && IsZero(accuracy.InaccuracyCrouch, mode)
                       && IsZero(accuracy.InaccuracyMove, mode)
                       && IsZero(accuracy.InaccuracyFire, mode)
                       && IsZero(accuracy.InaccuracyJump, mode)
                       && IsZero(accuracy.InaccuracyLand, mode)
                       && IsZero(accuracy.InaccuracyLadder, mode)
                       && accuracy.InaccuracyJumpInitial == 0f
                       && accuracy.InaccuracyJumpApex == 0f
                       && accuracy.InaccuracyReload == 0f;

        return (noRecoil, noSpread);
    }

    internal static void ResetRecoil(CCSPlayer_AimPunchServices aimPunch)
    {
        // VData не сбрасывает уже накопленный увод и скорость его изменения.
        // Непредсказуемый импульс от получения урона относится к другой механике.
        if (aimPunch.PredictableBaseTick.Value == -1
            && aimPunch.PredictableBaseTickInterpAmount == 0f
            && aimPunch.PredictableBaseAngle == QAngle.Zero
            && aimPunch.PredictableBaseAngleVel == QAngle.Zero)
        {
            return;
        }

        aimPunch.PredictableBaseTick.Value = -1;
        aimPunch.PredictableBaseTickInterpAmount = 0f;
        aimPunch.PredictableBaseAngle = QAngle.Zero;
        aimPunch.PredictableBaseAngleVel = QAngle.Zero;
        aimPunch.PredictableBaseTickUpdated();
        aimPunch.PredictableBaseTickInterpAmountUpdated();
        aimPunch.PredictableBaseAngleUpdated();
        aimPunch.PredictableBaseAngleVelUpdated();
    }

    internal static void ResetAccuracy(CCSWeaponBase weapon)
    {
        if (weapon.AccuracyPenalty != 0f)
        {
            weapon.AccuracyPenalty = 0f;
            weapon.AccuracyPenaltyUpdated();
        }

        weapon.TurningInaccuracy = 0f;
        weapon.TurningInaccuracyDelta = 0f;
        weapon.AccuracySmoothedForZoom = 0f;
    }

    private static bool IsZero(IReadOnlyList<float> values, int mode) =>
        mode is >= 0 and < 2 && mode < values.Count && values[mode] == 0f;
}
