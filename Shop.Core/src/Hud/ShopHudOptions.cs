namespace Shop.Core.Hud;

internal sealed class ShopHudOptions
{
    public bool Enabled { get; set; } = true;
    public bool ReplaceNativeBuyMenu { get; set; } = true;
    public bool HideNativeHudWhileOpen { get; set; } = true;
    public float RefreshIntervalSeconds { get; set; } = 0.25f;
    public int IdleTimeoutSeconds { get; set; } = 60;
}
