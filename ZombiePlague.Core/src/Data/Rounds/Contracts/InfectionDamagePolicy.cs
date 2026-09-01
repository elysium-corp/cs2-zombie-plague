using SwiftlyS2.Shared.SchemaDefinitions;
using ZombiePlague.Core.Config.Core;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal enum InfectionKnifeHitOutcome
{
    AbsorbWithArmor,
    Infect,
    DamageLastHuman
}

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
        var isSecondaryAttack = weaponMode == CSWeaponMode.Secondary_Mode;
        var configuredDamage = isSecondaryAttack
            ? config.ZombieSecondaryAttackArmorDamage
            : config.ZombiePrimaryAttackArmorDamage;

        return configuredDamage > 0
            ? configuredDamage
            : isSecondaryAttack ? 2 : 1;
    }

    internal static int CalculateRemainingArmor(int currentArmor, int armorDamage)
    {
        return Math.Max(0, currentArmor - Math.Max(0, armorDamage));
    }

    internal static InfectionKnifeHitOutcome ResolveKnifeHit(
        int currentArmor,
        int aliveHumanCount
    )
    {
        if (aliveHumanCount <= 1)
        {
            return InfectionKnifeHitOutcome.DamageLastHuman;
        }

        return currentArmor > 0
            ? InfectionKnifeHitOutcome.AbsorbWithArmor
            : InfectionKnifeHitOutcome.Infect;
    }
}
