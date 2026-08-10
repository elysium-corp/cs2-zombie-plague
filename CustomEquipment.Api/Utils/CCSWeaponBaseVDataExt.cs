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
}