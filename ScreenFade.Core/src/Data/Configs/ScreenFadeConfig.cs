namespace ScreenFade.Data.Configs;

internal sealed class ScreenFadeConfig
{
    // - время когда эффект будет появляться и потухать (120 мс тратится на появление эффекта и 120 мс на растворение) 
    public uint DurationMs => 120;

    // - время сколько эффект задержится на экране (Duration + holdTime + Duration = общее время эффекта на экране)
    public uint HoldTimeMs => 75;

    // - кол-во красного в цвете эффекта от 0..255
    public byte Red => 0;
    
    // - кол-во зеленного в цвете эффекта от 0..255
    public byte Green => 0;
    
    // - кол-во синего в цвете эффекта от 0..255
    public byte Blue => 255;
    
    // - прозрачность эффекта от 0..255
    public byte Alpha => 80;
}