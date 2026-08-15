namespace Economy.Core.Data.Configs;

internal sealed class EconomyConfig
{
    // - максимальное количество денег
    public int MaxMoney => 99_999;
    
    // - стартовое количество денег
    public int StartMoney => 5_000;
    
    // - количество денег за заражение
    public int MoneyForInfection => 500;
    
    // - количество денег за 1 единицу урона
    public float MoneyForDamage => 0.5f;
}