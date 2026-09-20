using System.Globalization;
using CustomKnife.Data.Models;

namespace CustomKnife.Hud;

// Слот подтверждения привязан к показанному ножу. Старый клик не экипирует
// новое выделение, а смена страницы/каталога требует новой сущности HUD.
internal sealed class KnifeHudSelection
{
    public const int PageSize = 7;
    public IReadOnlyList<IKnife> Catalog { get; private set; } = [];
    public int Page { get; private set; }
    public int SelectedSlot { get; private set; }
    public int PageCount => Math.Max(1, (Catalog.Count + PageSize - 1) / PageSize);
    public IKnife? At(int slot) => slot is >= 0 and < PageSize
        ? Catalog.ElementAtOrDefault(Page * PageSize + slot) : null;
    public IKnife? Selected => At(SelectedSlot);

    public bool Bind(IReadOnlyList<IKnife> knives, string equipped)
    {
        if (Catalog.Count == knives.Count && Catalog.Zip(knives).All(pair => ReferenceEquals(pair.First, pair.Second)))
            return false;
        var previous = Selected?.InternalName ?? equipped;
        Catalog = knives;
        var index = Array.FindIndex(knives.ToArray(), knife => knife.InternalName == previous);
        Page = index >= 0 ? index / PageSize : Math.Min(Page, PageCount - 1);
        SelectedSlot = index >= 0 ? index % PageSize : 0;
        return true;
    }

    public bool Move(int direction)
    {
        var next = Math.Clamp(Page + direction, 0, PageCount - 1);
        if (next == Page) return false;
        Page = next;
        SelectedSlot = 0;
        return true;
    }

    public bool Preview(int slot)
    {
        if (At(slot) is null) return false;
        SelectedSlot = slot;
        return true;
    }

    public bool CanConfirm(int slot) => slot == SelectedSlot && At(slot) is not null;

    public static bool TrySlot(string button, string prefix, out int slot)
    {
        slot = -1;
        return button.StartsWith(prefix, StringComparison.Ordinal)
            && int.TryParse(button.AsSpan(prefix.Length), NumberStyles.None, CultureInfo.InvariantCulture, out slot)
            && slot is >= 0 and < PageSize
            && button == prefix + slot.ToString(CultureInfo.InvariantCulture);
    }
}
