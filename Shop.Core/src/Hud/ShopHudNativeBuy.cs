namespace Shop.Core.Hud;

internal enum ShopNativeBuyAction { None, CloseNative, OpenCustom, TimedOut }

// buymenu переключает клиентское окно. Отправляем её ровно один раз для
// подтверждённого открытого окна и не захватываем мышь до подтверждения закрытия.
internal sealed class ShopHudNativeBuy
{
    public bool WasOpen { get; private set; }
    public bool Waiting { get; private set; }
    public bool OpenAfterClose { get; private set; }
    private double _deadline;

    public ShopNativeBuyAction Request(bool openAfterClose, double now)
    {
        OpenAfterClose = openAfterClose;
        if (Waiting) return ShopNativeBuyAction.None;
        WasOpen = true;
        Waiting = true;
        _deadline = now + 2;
        return ShopNativeBuyAction.CloseNative;
    }

    public void CancelOpen() => OpenAfterClose = false;

    public ShopNativeBuyAction Observe(bool nativeOpen, bool customOpen, double now)
    {
        var rising = nativeOpen && !WasOpen;
        WasOpen = nativeOpen;
        if (Waiting)
        {
            if (!nativeOpen)
            {
                Waiting = false;
                var open = OpenAfterClose;
                OpenAfterClose = false;
                return open ? ShopNativeBuyAction.OpenCustom : ShopNativeBuyAction.None;
            }
            if (now >= _deadline)
            {
                Waiting = false;
                OpenAfterClose = false;
                return ShopNativeBuyAction.TimedOut;
            }
            return ShopNativeBuyAction.None;
        }
        return rising ? Request(!customOpen, now) : ShopNativeBuyAction.None;
    }
}
