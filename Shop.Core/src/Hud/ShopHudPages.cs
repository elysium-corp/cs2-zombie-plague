namespace Shop.Core.Hud;

// Кнопки двух последовательных страниц имеют разные ID. Навигация тоже входит
// в набор: очередь кликов старой страницы не может перелистнуть новую обратно.
internal sealed class ShopHudPages : IDisposable
{
    public IShopHudRuntime? Runtime { get; private set; }
    public string Bank { get; private set; } = "A";
    private ShopHudView? _view;
    private int _navigationVersion;

    public bool Bind(ShopHudView view, int navigationVersion, Func<IShopHudRuntime> create)
    {
        var created = false;
        if (Runtime is not null && !ShopHudMenu.SameSlots(_view, view))
        {
            if (_navigationVersion != navigationVersion) Bank = Bank == "A" ? "B" : "A";
            else
            {
                // Перезагрузка каталога или удаление регистрации могут происходить
                // без кликов игрока. Не переиспользуем A/B после таких обновлений:
                // два быстрых reload иначе сделали бы очень старый клик допустимым.
                Runtime.Dispose();
                Runtime = null;
                Bank = "A";
            }
        }
        if (Runtime is null) { Runtime = create(); created = true; }
        _view = view;
        _navigationVersion = navigationVersion;
        return created;
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
