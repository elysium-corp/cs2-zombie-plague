namespace Economy.Core.Services;

internal static class EconomyRewardCalculator
{
    public static int CalculateDamageReward(
        int actualDamage,
        decimal moneyPerDamage,
        decimal roleBonusPercent,
        decimal weaponBonusPercent)
    {
        if (actualDamage <= 0 || moneyPerDamage <= 0m)
        {
            return 0;
        }

        var reward = actualDamage
                     * moneyPerDamage
                     * Multiplier(roleBonusPercent)
                     * Multiplier(weaponBonusPercent);

        return ToMoney(reward);
    }

    public static int CalculateFlatReward(int baseReward, decimal roleBonusPercent)
    {
        if (baseReward <= 0)
        {
            return 0;
        }

        return ToMoney(baseReward * Multiplier(roleBonusPercent));
    }

    private static decimal Multiplier(decimal bonusPercent)
    {
        return 1m + Math.Max(0m, bonusPercent) / 100m;
    }

    private static int ToMoney(decimal value)
    {
        return (int)Math.Clamp(decimal.Floor(value), 0m, int.MaxValue);
    }
}
