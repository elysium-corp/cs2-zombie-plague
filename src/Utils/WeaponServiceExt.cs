using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Utils;

public static class WeaponServiceExt
{
    extension(CCSPlayer_WeaponServices weaponService)
    {
        public CBasePlayerWeapon? FindWeaponByIndex(uint index)
        {
            ref var vec  = ref weaponService.MyWeapons;

            foreach (var handle in vec)
            {
                if (!handle.IsValid)
                {
                    continue;
                }

                var weapon = handle.Value;

                if (weapon == null)
                {
                    continue;
                }

                if (weapon.Index == index)
                {
                    return weapon;
                }
            }

            return null;
        }
        
        public CBasePlayerWeapon? FindWeaponByIndex(int index)
        {
            ref var vec  = ref weaponService.MyWeapons;

            foreach (var handle in vec)
            {
                if (!handle.IsValid)
                {
                    continue;
                }

                var weapon = handle.Value;

                if (weapon == null)
                {
                    continue;
                }

                if (weapon.Index == index)
                {
                    return weapon;
                }
            }
            
            return null;
        }

        public HashSet<int> MyWeaponsAsIds()
        {
            var result = new HashSet<int>();

            foreach (var wp in weaponService.MyWeapons)
            {
                var index = wp.Value?.Index;
                if (index.HasValue)
                {
                    result.Add((int)index.Value);
                }
            }

            return result;
        }
    }
}