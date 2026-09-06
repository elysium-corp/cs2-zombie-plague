using System.Globalization;
using System.Text;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Experimental.AbilityHud;

internal sealed record AbilityHudIcon(string Key, string Name, string Kind, string State, string Countdown, string Hotkey);

internal sealed record AbilityHudFrame(AbilityHudIcon[] Icons, int Overflow, bool Human, bool ShowNames)
{
    public const int SlotCount = 12;
    public static readonly string[] Kinds = ["heal", "leap", "blind", "charge", "trap", "catch", "double_jump", "generic"];
    public static readonly AbilityHudFrame Empty = new([], 0, false, false);

    public static AbilityHudFrame Create(IEnumerable<IAbility> abilities, int maximum, bool human, bool showNames)
    {
        var icons = new List<AbilityHudIcon>();
        var keys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var ability in abilities)
        {
            // Берём метаданные экземпляра роли, а не более новый снимок каталога после reload
            if (ability is not IPresentedAbility { Presentation: { } info } || !keys.Add(info.Key)) continue;
            var passive = ability is IPassiveAbility;
            var remaining = ability is ICooldownRestricted cooldown ? cooldown.RemainingCooldown : 0;
            var seconds = float.IsFinite(remaining) ? (int)Math.Ceiling(Math.Clamp(remaining, 0, 3600)) : 0;
            var kind = Kinds.Contains(info.Kind, StringComparer.Ordinal) ? info.Kind : "generic";
            var name = string.Concat(info.Name.EnumerateRunes().Take(24).Select(rune => rune.ToString()));
            var key = passive ? "" : kind == "leap" ? "CTRL+SPACE" : "E";
            icons.Add(new(info.Key, name, kind, passive ? "Passive" : seconds > 0 ? "Cooling" : "Ready",
                !passive && seconds > 0 ? seconds.ToString(CultureInfo.InvariantCulture) : "", key));
        }
        maximum = Math.Clamp(maximum, 1, SlotCount);
        return new(icons.Take(maximum).ToArray(), Math.Max(0, icons.Count - maximum), human, showNames);
    }
}
