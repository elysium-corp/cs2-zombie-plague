using System.Globalization;
using CustomKnife.Data.Models;

namespace CustomKnife.Hud;

// Сравниваются параметры выбранного и сохранённого человеческого ножа.
// Цвет отражает направление числового изменения, включая гравитацию.
internal readonly record struct KnifeHudStat(string Key, double Current, double Selected, bool Multiplier = false)
{
    private bool IsFinite => double.IsFinite(Current) && double.IsFinite(Selected);
    public string Direction => !IsFinite || Math.Abs(Selected - Current) < 0.00001
        ? "Unchanged" : Selected > Current ? "Increase" : "Decrease";

    public string CurrentText(CultureInfo culture) => Format(Current, culture);
    public string SelectedText(CultureInfo culture) => Format(Selected, culture);

    public string DeltaText(CultureInfo culture)
    {
        if (!IsFinite) return "—";
        if (Direction == "Unchanged") return "0%";
        var difference = Selected - Current;
        // Процент относительно нуля не определён: показываем абсолютную разницу.
        var value = Current == 0 ? difference : difference / Math.Abs(Current) * 100;
        return (value > 0 ? "+" : "−") + Math.Abs(value).ToString("0.##", culture)
            + (Current == 0 ? "" : "%");
    }

    private string Format(double value, CultureInfo culture) => double.IsFinite(value)
        ? (Multiplier ? "×" : "") + value.ToString("0.##", culture) : "—";
}

internal static class KnifeHudComparison
{
    public static KnifeHudStat[] Compare(IKnife current, IKnife selected) =>
    [
        new("Speed", current.Speed, selected.Speed),
        new("Damage", current.DamageMultiplier, selected.DamageMultiplier, true),
        new("Gravity", current.Gravity, selected.Gravity),
        new("Knockback", current.KnockbackData.Recoil, selected.KnockbackData.Recoil)
    ];
}
