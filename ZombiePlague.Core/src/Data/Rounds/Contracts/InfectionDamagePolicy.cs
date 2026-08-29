using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Config.Core;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal static class InfectionDamagePolicy
{
    internal static bool IsKnifeAttack(DamageTypes_t damageType, string? activeWeaponName)
    {
        return (damageType & DamageTypes_t.DMG_SLASH) != 0 &&
               activeWeaponName?.Contains("knife", StringComparison.OrdinalIgnoreCase) == true;
    }

    internal static int GetArmorDamage(
        CSWeaponMode weaponMode,
        ZombiePlagueCoreConfig config
    )
    {
        var configuredDamage = weaponMode == CSWeaponMode.Secondary_Mode
            ? config.ZombieSecondaryAttackArmorDamage
            : config.ZombiePrimaryAttackArmorDamage;

        return Math.Max(0, configuredDamage);
    }

    internal static int CalculateRemainingArmor(int currentArmor, int armorDamage)
    {
        return Math.Max(0, currentArmor - Math.Max(0, armorDamage));
    }
}
