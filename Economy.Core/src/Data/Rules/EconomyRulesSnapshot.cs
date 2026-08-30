using Economy.Core.Data.Configs;
using Economy.Core.Database.Entities;

namespace Economy.Core.Data.Rules;

internal sealed record EconomyRoleRule(
    string PrivilegeKey,
    int MaxMoney,
    decimal RewardBonusPercent
);

internal sealed record EconomyWeaponRule(
    string WeaponKey,
    decimal DamageBonusPercent
);

internal sealed record EconomyPersistenceRules(
    bool SaveOnRoundEnd,
    bool SaveOnDisconnect,
    bool SaveOnUnload,
    bool PeriodicSaveEnabled,
    TimeSpan PeriodicSaveInterval
);

internal sealed record EconomyRulesSnapshot(
    long Revision,
    int AbsoluteMaxMoney,
    int DefaultMaxMoney,
    int StartMoney,
    decimal MoneyPerDamage,
    int MoneyForInfection,
    int MoneyForZombieKill,
    int MoneyForHumanKill,
    TimeSpan SettingsRefreshInterval,
    EconomyPersistenceRules Persistence,
    IReadOnlyDictionary<string, EconomyRoleRule> RoleRules,
    IReadOnlyDictionary<string, EconomyWeaponRule> WeaponRules
)
{
    private const int MaximumMoney = 2_147_483_647;
    private const int MinimumRefreshSeconds = 10;
    private const int MaximumRefreshSeconds = 3_600;
    private const int MinimumSaveSeconds = 10;
    private const int MaximumSaveSeconds = 86_400;
    private const decimal MaximumRewardRate = 1_000_000m;
    private const decimal MaximumBonusPercent = 10_000m;

    public static EconomyRulesSnapshot FromConfig(EconomyConfig? config)
    {
        config ??= new EconomyConfig();
        var rules = config.Rules ?? new EconomyRulesConfig();

        return Create(
            revision: 0,
            absoluteMaxMoney: rules.AbsoluteMaxMoney,
            defaultMaxMoney: rules.DefaultMaxMoney,
            startMoney: rules.StartMoney,
            moneyPerDamage: rules.MoneyPerDamage,
            moneyForInfection: rules.MoneyForInfection,
            moneyForZombieKill: rules.MoneyForZombieKill,
            moneyForHumanKill: rules.MoneyForHumanKill,
            settingsRefreshIntervalSeconds: config.SettingsRefreshIntervalSeconds,
            persistence: rules.Persistence,
            roleRules: rules.RoleRules,
            weaponRules: rules.WeaponRules
        );
    }

    public static EconomyRulesSnapshot FromDatabase(
        EconomySettingsEntity settings,
        IReadOnlyCollection<EconomyRoleRuleEntity> roleRules,
        IReadOnlyCollection<EconomyWeaponRuleEntity> weaponRules)
    {
        return Create(
            settings.Revision,
            settings.AbsoluteMaxMoney,
            settings.DefaultMaxMoney,
            settings.StartMoney,
            settings.MoneyPerDamage,
            settings.MoneyForInfection,
            settings.MoneyForZombieKill,
            settings.MoneyForHumanKill,
            settings.SettingsRefreshIntervalSeconds,
            new EconomyPersistenceConfig
            {
                SaveOnRoundEnd = settings.SaveOnRoundEnd,
                SaveOnDisconnect = settings.SaveOnDisconnect,
                SaveOnUnload = settings.SaveOnUnload,
                PeriodicSaveEnabled = settings.PeriodicSaveEnabled,
                PeriodicSaveIntervalSeconds = settings.PeriodicSaveIntervalSeconds
            },
            roleRules.Select(rule => new EconomyRoleRuleConfig
            {
                PrivilegeKey = rule.PrivilegeKey,
                MaxMoney = rule.MaxMoney,
                RewardBonusPercent = rule.RewardBonusPercent,
                Enabled = rule.Enabled
            }),
            weaponRules.Select(rule => new EconomyWeaponRuleConfig
            {
                WeaponKey = rule.WeaponKey,
                DamageBonusPercent = rule.DamageBonusPercent,
                Enabled = rule.Enabled
            })
        );
    }

    public static string NormalizePrivilegeKey(string value)
    {
        return value.Trim().ToLowerInvariant();
    }

    public static string NormalizeWeaponKey(string value)
    {
        var normalized = value.Trim().ToLowerInvariant();

        return normalized.StartsWith("weapon_", StringComparison.Ordinal)
            ? normalized["weapon_".Length..]
            : normalized;
    }

    private static EconomyRulesSnapshot Create(
        long revision,
        int absoluteMaxMoney,
        int defaultMaxMoney,
        int startMoney,
        decimal moneyPerDamage,
        int moneyForInfection,
        int moneyForZombieKill,
        int moneyForHumanKill,
        int settingsRefreshIntervalSeconds,
        EconomyPersistenceConfig? persistence,
        IEnumerable<EconomyRoleRuleConfig>? roleRules,
        IEnumerable<EconomyWeaponRuleConfig>? weaponRules)
    {
        var absoluteLimit = Math.Clamp(absoluteMaxMoney, 0, MaximumMoney);
        var defaultLimit = Math.Clamp(defaultMaxMoney, 0, absoluteLimit);
        var normalizedPersistence = persistence ?? new EconomyPersistenceConfig();

        var normalizedRoleRules = (roleRules ?? [])
            .Where(rule => rule.Enabled && !string.IsNullOrWhiteSpace(rule.PrivilegeKey))
            .GroupBy(rule => NormalizePrivilegeKey(rule.PrivilegeKey), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group =>
                {
                    var maximum = group.Max(rule => Math.Clamp(rule.MaxMoney, 0, absoluteLimit));
                    var bonus = group.Max(rule => Math.Clamp(rule.RewardBonusPercent, 0m, MaximumBonusPercent));
                    return new EconomyRoleRule(group.Key, maximum, bonus);
                },
                StringComparer.OrdinalIgnoreCase
            );

        var normalizedWeaponRules = (weaponRules ?? [])
            .Where(rule => rule.Enabled && !string.IsNullOrWhiteSpace(rule.WeaponKey))
            .GroupBy(rule => NormalizeWeaponKey(rule.WeaponKey), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => new EconomyWeaponRule(
                    group.Key,
                    group.Max(rule => Math.Clamp(rule.DamageBonusPercent, 0m, MaximumBonusPercent))
                ),
                StringComparer.OrdinalIgnoreCase
            );

        return new EconomyRulesSnapshot(
            Math.Max(0L, revision),
            absoluteLimit,
            defaultLimit,
            Math.Clamp(startMoney, 0, defaultLimit),
            Math.Clamp(moneyPerDamage, 0m, MaximumRewardRate),
            Math.Clamp(moneyForInfection, 0, MaximumMoney),
            Math.Clamp(moneyForZombieKill, 0, MaximumMoney),
            Math.Clamp(moneyForHumanKill, 0, MaximumMoney),
            TimeSpan.FromSeconds(Math.Clamp(
                settingsRefreshIntervalSeconds,
                MinimumRefreshSeconds,
                MaximumRefreshSeconds
            )),
            new EconomyPersistenceRules(
                normalizedPersistence.SaveOnRoundEnd,
                normalizedPersistence.SaveOnDisconnect,
                normalizedPersistence.SaveOnUnload,
                normalizedPersistence.PeriodicSaveEnabled,
                TimeSpan.FromSeconds(Math.Clamp(
                    normalizedPersistence.PeriodicSaveIntervalSeconds,
                    MinimumSaveSeconds,
                    MaximumSaveSeconds
                ))
            ),
            normalizedRoleRules,
            normalizedWeaponRules
        );
    }
}
