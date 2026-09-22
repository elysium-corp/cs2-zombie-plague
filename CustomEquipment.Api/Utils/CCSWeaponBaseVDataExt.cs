using CustomEquipment.Api.Data.Models;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Utils;

internal static class CcsWeaponBaseVDataExt
{
    extension(CCSWeaponBaseVData data)
    {
        internal void SetRecoil(WeaponRecoil? recoil)
        {
            if (recoil is null) return;

            SetModes(data.RecoilMagnitude, recoil.Magnitude);
            SetModes(data.RecoilMagnitudeVariance, recoil.MagnitudeVariance);
            SetModes(data.RecoilAngle, recoil.Angle);
            SetModes(data.RecoilAngleVariance, recoil.AngleVariance);
        }

        internal void SetAccuracy(WeaponAccuracy? accuracy)
        {
            if (accuracy is null) return;

            SetModes(data.Spread, accuracy.Spread);
            SetModes(data.InaccuracyStand, accuracy.InaccuracyStand);
            SetModes(data.InaccuracyCrouch, accuracy.InaccuracyCrouch);
            SetModes(data.InaccuracyMove, accuracy.InaccuracyMove);
            SetModes(data.InaccuracyFire, accuracy.InaccuracyFire);
            SetModes(data.InaccuracyJump, accuracy.InaccuracyJump);
            SetModes(data.InaccuracyLand, accuracy.InaccuracyLand);
            SetModes(data.InaccuracyLadder, accuracy.InaccuracyLadder);

            if (accuracy.InaccuracyJumpInitial is { } jumpInitial) data.InaccuracyJumpInitial = jumpInitial;
            if (accuracy.InaccuracyJumpApex is { } jumpApex) data.InaccuracyJumpApex = jumpApex;
            if (accuracy.InaccuracyReload is { } reload) data.InaccuracyReload = reload;
        }

        internal void SetAmmo(int? clip, int? reserve, CCSWeaponBase? weapon)
        {
            if (clip != null)
            {
                var maxClip = (int)clip;

                data.MaxClip1 = maxClip;
                data.DefaultClip1 = maxClip;

                var playerWeaponVData = weapon?.PlayerWeaponVData;
                
                playerWeaponVData?.MaxClip1 = maxClip;
                weapon?.Clip1 = maxClip;
                weapon?.Clip1Updated();
            }

            if (reserve != null)
            {
                var reserveAmmo = (int)reserve;

                data.PrimaryReserveAmmoMax = reserveAmmo;
                data.SecondaryReserveAmmoMax = reserveAmmo;
                
                weapon?.ReserveAmmo[0] = reserveAmmo;
                weapon?.ReserveAmmo[1] = reserveAmmo;
                
                weapon?.ReserveAmmoUpdated();
            }
        }

        internal void SetDamage(int? bullets, float? penetration, float? range, float? rangeModifier)
        {
            if (bullets != null)
            {
                data.NumBullets = (int)bullets;
            }

            if (penetration != null) data.Penetration = (float)penetration;

            if (range != null) data.Range = (float)range;

            if (rangeModifier != null) data.RangeModifier = (float)rangeModifier;
        }

        internal void SetTiming(List<float>? cycleTime, float? deployDuration, CCSWeaponBase? weapon)
        {
            if (cycleTime != null)
            {
                var vDataCycleTime = data.CycleTime.Values;

                for (byte index = 0; index < cycleTime.Count; index++)
                {
                    vDataCycleTime[index] = cycleTime[index];
                }
            }

            if (deployDuration != null) data.DeployDuration = (float)deployDuration;
        }
    }

    private static void SetModes(CFiringModeFloat target, IReadOnlyList<float> values)
    {
        var modes = target.Values;

        for (var index = 0; index < values.Count; index++)
        {
            modes[index] = values[index];
        }
    }
}
