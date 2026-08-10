using SwiftlyS2.Shared.Menus;

namespace Menu.Api.Data;

public sealed class MenuOptionsCollection
{
    private readonly List<MenuOptionEntry> _options = [];

    public void Add(IMenuOption option, int priority = int.MaxValue)
    {
        var optionEntry = new MenuOptionEntry(option, priority);
        _options.Add(optionEntry);
    }

    internal IEnumerable<IMenuOption> Build()
    {
        return _options
            .OrderBy(entry => entry.Priority)
            .Select(entry => entry.Option);
    }

    private sealed record MenuOptionEntry(
        IMenuOption Option,
        int Priority
    );
}