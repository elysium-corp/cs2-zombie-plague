using Shop.Core.Data;

namespace Shop.Core.Hud;

// Кнопки двух последовательных страниц имеют разные ID. Навигация тоже входит
// в набор: очередь кликов старой страницы не может перелистнуть новую обратно.
internal sealed class ShopHudPages : IDisposable
{
    public IShopHudRuntime? Runtime { get; private set; }
    public string Bank { get; private set; } = "A";
    private ShopHudView? _view;
    private ShopSnapshot? _snapshot;

    public void Bind(ShopHudView view, ShopSnapshot snapshot, Func<IShopHudRuntime> create)
    {
        if (Runtime is null) Runtime = create();
        else if (!ShopHudMenu.SameSlots(_view, view) || !ReferenceEquals(_snapshot, snapshot))
            Bank = Bank == "A" ? "B" : "A";
        _view = view;
        _snapshot = snapshot;
    }

    public bool TryButton(string raw, out string button)
    {
        button = string.Empty;
        if (!raw.StartsWith(Bank + "_", StringComparison.Ordinal)) return false;
        button = raw[2..];
        return true;
    }

    public void Dispose()
    {
        Runtime?.Dispose();
        Runtime = null;
    }
}
