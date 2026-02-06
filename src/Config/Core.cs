namespace CS2ZombiePlague.Config;

public sealed class ZombiePlagueCoreConfig
{
    public bool DamageNotifyEnabled { get; set; } = true;
    public int DamageNotifyDuration { get; set; } = 1000;

    public bool KnockbackEnabled { get; set; } = true;
    
    public bool RoundRatingNotify { get; set; } = true;
    
    // Включить денежную систему
    public bool MoneySystemEnabled { get; set; } = true;
    // Максимальное количество денег
    public int MaxMoney { get; set; } = 100000;
    // Стартовое количество денег
    public int StartMoney { get; set; } = 5000;
    // Количество денег за заражение
    public int MoneyForInfection { get; set; } = 500;

    public int PreStartDelay { get; set; } = 20;
    public int ZombieSpawnDelay { get; set; } = 5;
    
    // Включить подсветку экрана после убийства
    public bool ScreenFadeEnable { get; set; } = true;
    
    // Время когда эффект будет появляться и потухать (120 мс тратится на появление эффекта и 120 мс на растворение) 
    public uint DurationScreenFade { get; set; } = 120;

    // Время сколько эффект задержится на экране (Duration + holdTime + Duration = общее время эффекта на экране)
    public uint HoldTimeScreenFade { get; set; } = 75;

    // Кол-во красного в цвете эффекта от 0..255
    public byte RedColorScreenFade { get; set; } = 0;
    
    // Кол-во зеленного в цвете эффекта от 0..255
    public byte GreenColorScreenFade{ get; set; } = 0;
    
    // Кол-во синего в цвете эффекта от 0..255
    public byte BlueColorScreenFade { get; set; } = 255;
    
    // Прозрачность эффекта от 0..255
    public byte AlphaScreenFade { get; set; } = 80;
}