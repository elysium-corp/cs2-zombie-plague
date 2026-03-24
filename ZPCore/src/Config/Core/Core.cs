namespace ZPCore.Config.Core;

public sealed class ZombiePlagueCoreConfig
{
    // Включить отталкивание зомби
    public bool KnockbackEnabled { get; set; } = true;
    // Множитель отталкивания зомби в голову
    public float KnockbackHeadMultiply { get; set; } = 2.0f;
    // Множитель отталкивания зомби в тело
    public float KnockbackBodyMultiply { get; set; } = 1.0f;
    // Высота подбрасывания зомби в воздухе после попадания
    public float AirKnockback { get; set; } = 25.0f;
    // Высота подбрасывания зомби на земле после попадания(меньше 150 не работает)
    public float GroundKnockback { get; set; } = 150.0f;
    // Минимальная сила отдачи для отталкивания
    public float MinKnockbackForce { get; set; } = 75.0f;
    
    // Включить рэйтинг игроков за раунд
    public bool RoundRatingNotify { get; set; } = true;
    
    // Время до начала заражения
    public int PreStartDelay { get; set; } = 20;
    // Время возрождения зомби
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