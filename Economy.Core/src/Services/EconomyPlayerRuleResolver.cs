using Economy.Core.Data.Rules;
using SwiftlyS2.Shared.Players;

namespace Economy.Core.Services;

internal sealed class EconomyPlayerRuleResolver(
    IEconomyRulesProvider rulesProvider,
    EconomyExternalApis externalApis
)
{
    public EconomyPlayerModifiers Resolve(IPlayer player)
    {
        ArgumentNullException.ThrowIfNull(player);

        var rules = rulesProvider.Current;
        var maxMoney = rules.DefaultMaxMoney;
        var rewardBonusPercent = 0m;
        var adminApi = externalApis.Admin;

        if (adminApi is null || rules.RoleRules.Count == 0)
        {
            return new EconomyPlayerModifiers(maxMoney, rewardBonusPercent);
        }

        foreach (var privilege in adminApi.GetPlayerPrivileges(player))
        {
            var key = EconomyRulesSnapshot.NormalizePrivilegeKey(privilege.Key);

            if (!rules.RoleRules.TryGetValue(key, out var roleRule))
            {
                continue;
            }

            maxMoney = Math.Max(maxMoney, roleRule.MaxMoney);
            rewardBonusPercent = Math.Max(rewardBonusPercent, roleRule.RewardBonusPercent);
        }

        return new EconomyPlayerModifiers(
            Math.Min(maxMoney, rules.AbsoluteMaxMoney),
            rewardBonusPercent
        );
    }
}
