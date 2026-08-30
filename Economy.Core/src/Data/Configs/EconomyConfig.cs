namespace Economy.Core.Data.Configs;

internal sealed class EconomyConfig
{
    public int SchemaVersion { get; set; } = 2;

    public int SettingsRefreshIntervalSeconds { get; set; } = 30;

    public EconomyRulesConfig Rules { get; set; } = new();
}

internal sealed class EconomyRulesConfig
{
    public int AbsoluteMaxMoney { get; set; } = 99_999;

    public int DefaultMaxMoney { get; set; } = 99_999;

    public int StartMoney { get; set; } = 5_000;

    public decimal MoneyPerDamage { get; set; } = 0.5m;

    public int MoneyForInfection { get; set; } = 500;

    public int MoneyForZombieKill { get; set; }

    public int MoneyForHumanKill { get; set; }

    public EconomyPersistenceConfig Persistence { get; set; } = new();

    public List<EconomyRoleRuleConfig> RoleRules { get; set; } = [];

    public List<EconomyWeaponRuleConfig> WeaponRules { get; set; } = [];
}

internal sealed class EconomyPersistenceConfig
{
    public bool SaveOnRoundEnd { get; set; }

    public bool SaveOnDisconnect { get; set; } = true;

    public bool SaveOnUnload { get; set; } = true;

    public bool PeriodicSaveEnabled { get; set; }

    public int PeriodicSaveIntervalSeconds { get; set; } = 300;
}

internal sealed class EconomyRoleRuleConfig
{
    public string PrivilegeKey { get; set; } = string.Empty;

    public int MaxMoney { get; set; } = 99_999;

    public decimal RewardBonusPercent { get; set; }

    public bool Enabled { get; set; } = true;
}

internal sealed class EconomyWeaponRuleConfig
{
    public string WeaponKey { get; set; } = string.Empty;

    public decimal DamageBonusPercent { get; set; }

    public bool Enabled { get; set; } = true;
}
