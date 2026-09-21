using CustomEquipment.Api.Data.Models;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CustomEquipment.Utils;

internal static class CcsWeaponBaseVDataExt
{
    extension(CCSWeaponBaseVData data)
    {
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

        internal void SetFiring(WeaponFiring? firing)
        {
            if (firing == null)
            {
                return;
            }

            SetFiringMode(data.Spread, firing.Spread);
            SetFiringMode(data.InaccuracyCrouch, firing.InaccuracyCrouch);
            SetFiringMode(data.InaccuracyStand, firing.InaccuracyStand);
            SetFiringMode(data.InaccuracyJump, firing.InaccuracyJump);
            SetFiringMode(data.InaccuracyLand, firing.InaccuracyLand);
            SetFiringMode(data.InaccuracyLadder, firing.InaccuracyLadder);
            SetFiringMode(data.InaccuracyFire, firing.InaccuracyFire);
            SetFiringMode(data.InaccuracyMove, firing.InaccuracyMove);
            SetFiringMode(data.RecoilAngle, firing.RecoilAngle);
            SetFiringMode(data.RecoilAngleVariance, firing.RecoilAngleVariance);
            SetFiringMode(data.RecoilMagnitude, firing.RecoilMagnitude);
            SetFiringMode(data.RecoilMagnitudeVariance, firing.RecoilMagnitudeVariance);
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

    private static void SetFiringMode(CFiringModeFloat data, IReadOnlyList<float> values)
    {
        var firingModeValues = data.Values;
        var count = Math.Min(values.Count, 2);

        for (byte index = 0; index < count; index++)
        {
            firingModeValues[index] = values[index];
        }
    }
}