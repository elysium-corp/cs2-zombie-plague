namespace Economy.Core.Data.Configs;

internal sealed class EconomyConfig
{
    // - максимальное количество денег
    public int MaxMoney => 100_000;
    
    // - стартовое количество денег
    public int StartMoney => 5_000;
    
    // - количество денег за заражение
    public int MoneyForInfection => 500;
}