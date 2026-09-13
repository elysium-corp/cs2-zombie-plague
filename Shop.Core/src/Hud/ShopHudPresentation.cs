namespace Shop.Core.Hud;

/// <summary>Закрепляет показ после CSS-триггера и не повторяет переключающую команду закрытия CS2.</summary>
internal sealed class ShopHudPresentation(bool followNative)
{
    public bool NativeTriggerEnabled { get; } = followNative;
    public bool Visible { get; private set; }
    public bool NativeCloseRequested { get; private set; }
    private bool _nativeWasOpen;
    private bool _cancelled;

    public bool IsOpen(bool nativeOpen) => Visible || NativeTriggerEnabled && !_cancelled && nativeOpen;

    public void Open(bool nativeOpen)
    {
        Visible = true;
        _cancelled = false;
        _nativeWasOpen = nativeOpen;
    }

    public bool ObserveNativeTrigger(bool nativeOpen)
    {
        var rising = NativeTriggerEnabled && !_cancelled && nativeOpen && !_nativeWasOpen;
        _nativeWasOpen = nativeOpen;
        if (!nativeOpen) NativeCloseRequested = false;
        return rising;
    }

    // Escape уже закрывает CS2 на клиенте: запоздалый сетевой флаг не должен
    // повторно открыть Shop или отправить переключающую buymenu в закрытое окно.
    public void CancelNativeTrigger() => _cancelled = true;

    // CSS уже показывает оверлей, затем сервер закрепляет Visible. Закрытие
    // CS2 не снимает этот флаг и не является условием для открытия магазина.
    public bool RequestNativeClose(bool nativeOpen)
    {
        if (_cancelled || !nativeOpen || NativeCloseRequested) return false;
        _nativeWasOpen = true;
        NativeCloseRequested = true;
        return true;
    }
}
