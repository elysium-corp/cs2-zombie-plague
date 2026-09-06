using System.Globalization;
using System.Text;
using SwiftlyS2.Shared.Menus;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Entities;
using ZombiePlague.Core.Data.Entities.Human;
using ZombiePlague.Core.Data.Entities.Zombie;
using ZombiePlague.Core.Store.Data;

namespace ZombiePlague.Core.Experimental.AbilityHud;

internal enum AbilityHudVisibility
{
    Ready, InvalidPlayer, Bot, Dead, MenuOpen, MissingRole, NoAbilities, MissingPresentation
}

internal sealed record AbilityHudIcon(string Key, string Name, string Kind, bool Passive, string State, string Countdown, string Hotkey);

internal sealed record AbilityHudFrame(AbilityHudIcon[] Icons, bool ShowNames)
{
    public AbilityHudPreferences Appearance { get; init; } = AbilityHudPreferences.Default;
    // Каталог допускает 16 способностей класса и 32 личных: показываем весь допустимый набор
    public const int SlotCount = 48;
    public const int IconsPerRow = 8;
    public const int MaximumRows = SlotCount / IconsPerRow;
    public int RowCount => (Icons.Length + IconsPerRow - 1) / IconsPerRow;
    public static readonly string[] Kinds = ["heal", "leap", "blind", "charge", "trap", "catch", "double_jump", "generic"];
    public static readonly AbilityHudFrame Empty = new([], false);

    internal static IReadOnlyList<IAbility> AbilitiesForRole(IPlayerRole? role) => role switch
    {
        IHuman human => human.HClass.Abilities,
        IZombie zombie => zombie.ZClass.Abilities,
        _ => []
    };

    public static AbilityHudFrame ForRole(IPlayerRole role, Func<string, string> localize, bool showNames) =>
        Create(AbilitiesForRole(role), localize, showNames);

    // Отрисовка и ручная диагностика используют одну проверку, чтобы причины скрытия не расходились
    internal static AbilityHudFrame ForPlayer(IPlayer player, IPlayerRole? role, IMenuAPI? menu,
        AbilityHudConfig config, Func<string, string> localize, out AbilityHudVisibility visibility)
    {
        visibility = !player.IsValid ? AbilityHudVisibility.InvalidPlayer
            : player.IsFakeClient ? AbilityHudVisibility.Bot
            : !player.IsAlive ? AbilityHudVisibility.Dead
            : AbilityHudSettings.ShouldHideForMenu(menu, config.HideWhenMenuOpen) ? AbilityHudVisibility.MenuOpen
            : role is null ? AbilityHudVisibility.MissingRole
            : AbilityHudVisibility.Ready;
        if (visibility != AbilityHudVisibility.Ready) return Empty;

        var abilities = AbilitiesForRole(role);
        var frame = Create(abilities, localize, config.ShowNames);
        if (frame.Icons.Length == 0)
            visibility = abilities.Count == 0 ? AbilityHudVisibility.NoAbilities : AbilityHudVisibility.MissingPresentation;
        return frame;
    }

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
