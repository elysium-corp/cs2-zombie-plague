using Economy.Core.Data.Rules;
using SwiftlyS2.Shared.Players;

namespace Economy.Core.Services;

internal sealed class EconomyRewardService(
    IEconomyRulesProvider rulesProvider,
    EconomyPlayerRuleResolver playerRuleResolver,
    IEconomyService economyService
)
{
    public void RewardDamage(IPlayer player, int actualDamage, string? weaponKey)
    {
        var rules = rulesProvider.Current;
        var modifiers = playerRuleResolver.Resolve(player);
        var weaponBonus = ResolveWeaponBonus(rules, weaponKey);
        var amount = EconomyRewardCalculator.CalculateDamageReward(
            actualDamage,
            rules.MoneyPerDamage,
            modifiers.RewardBonusPercent,
            weaponBonus
        );

        if (amount > 0)
        {
            economyService.GiveMoney(player, amount);
        }
    }

    public void RewardInfection(IPlayer player)
    {
        RewardFlat(player, rulesProvider.Current.MoneyForInfection);
    }

    public void RewardZombieKill(IPlayer player)
    {
        RewardFlat(player, rulesProvider.Current.MoneyForZombieKill);
    }

    public void RewardHumanKill(IPlayer player)
    {
        RewardFlat(player, rulesProvider.Current.MoneyForHumanKill);
    }

    private void RewardFlat(IPlayer player, int baseReward)
    {
        var modifiers = playerRuleResolver.Resolve(player);
        var amount = EconomyRewardCalculator.CalculateFlatReward(
            baseReward,
            modifiers.RewardBonusPercent
        );

        if (amount > 0)
        {
            economyService.GiveMoney(player, amount);
        }
    }

    private static decimal ResolveWeaponBonus(EconomyRulesSnapshot rules, string? weaponKey)
    {
        if (string.IsNullOrWhiteSpace(weaponKey))
        {
            return 0m;
        }

        var normalizedKey = EconomyRulesSnapshot.NormalizeWeaponKey(weaponKey);

        return rules.WeaponRules.TryGetValue(normalizedKey, out var rule)
            ? rule.DamageBonusPercent
            : 0m;
    }
}
