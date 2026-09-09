namespace Shop.Core.Hud;

/// <summary>Двухфазное перелистывание одной области без пересоздания HUD.</summary>
internal sealed class ShopHudTransition
{
    private Action? _swap;
    private double _deadline;
    private double _halfDuration;
    private string _panel = "";
    private string _direction = "Next";
    private bool _entering;

    public bool Busy { get; private set; }

    public string ClassFor(string panel) => Busy && panel == _panel
        ? "Motion" + (_entering ? "In" : "Out") + _direction : "MotionIdle";

    public void Begin(string panel, int direction, double now, ShopHudAppearance appearance, Action swap)
    {
        if (Busy) return;
        if (appearance.PageAnimation == "none") { swap(); return; }
        _panel = panel;
        _direction = direction > 0 ? "Next" : "Previous";
        _halfDuration = appearance.Duration / 2;
        _deadline = now + _halfDuration;
        _swap = swap;
        _entering = false;
        Busy = true;
    }

    public bool Advance(double now)
    {
        if (!Busy || now < _deadline) return false;
        if (!_entering)
        {
            _swap?.Invoke();
            _swap = null;
            _entering = true;
            // После задержки серверного тика всё равно показываем фазу появления.
            _deadline = now + _halfDuration;
        }
        else Busy = false;
        return true;
    }
}
