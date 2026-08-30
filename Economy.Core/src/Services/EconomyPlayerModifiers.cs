namespace Economy.Core.Services;

internal readonly record struct EconomyPlayerModifiers(
    int MaxMoney,
    decimal RewardBonusPercent
);
