using System.Globalization;
using System.Text;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Entities;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Zombie;

namespace ZombiePlague.Core.Experimental.AbilityHud;

internal sealed record AbilityHudIcon(string Key, string Name, string Kind, bool Passive, string State, string Countdown, string Hotkey);

internal sealed record AbilityHudFrame(AbilityHudIcon[] Icons, bool ShowNames)
{
    // Каталог допускает 16 способностей класса и 32 личных: показываем весь допустимый набор
    public const int SlotCount = 48;
    public const int IconsPerRow = 8;
    public const int MaximumRows = SlotCount / IconsPerRow;
    public int RowCount => (Icons.Length + IconsPerRow - 1) / IconsPerRow;
    public static readonly string[] Kinds = ["heal", "leap", "blind", "charge", "trap", "catch", "double_jump", "generic"];
    public static readonly AbilityHudFrame Empty = new([], false);

    public static AbilityHudFrame ForRole(IPlayerRole role, Func<string, string> localize, bool showNames) => Create(role switch
    {
        IHuman human => human.HClass.Abilities,
        IZombie zombie => zombie.ZClass.Abilities,
        _ => []
    }, localize, showNames);

    public static AbilityHudFrame Create(IEnumerable<IAbility> abilities, Func<string, string> localize, bool showNames)
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
            var name = showNames ? string.Concat(localize(info.NameKey).EnumerateRunes().Take(48).Select(rune => rune.ToString())) : "";
            var key = passive ? "∞" : ability is IActiveAbility active
                ? active.Key?.ToString().ToUpperInvariant() ?? (kind == "leap" ? "CTRL+SPACE" : "") : "";
            icons.Add(new(info.Key, name, kind, passive, seconds > 0 ? "Cooling" : "Ready",
                seconds > 0 ? seconds.ToString(CultureInfo.InvariantCulture) : "", key));
        }
        if (icons.Count > SlotCount)
            throw new InvalidDataException($"Набор способностей превышает {SlotCount} слотов HUD — требуется обновление Panorama");
        // Сначала активные, затем пассивные; состояние КД не меняет порядок иконок
        return new(icons.OrderBy(icon => icon.Passive).ToArray(), showNames);
    }
}
