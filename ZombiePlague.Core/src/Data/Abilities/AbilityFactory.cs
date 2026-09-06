using Localization.Api;
using SwiftlyS2.Shared;
using ZombiePlague.Core.Catalog;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Abilities;

internal sealed class AbilityFactory(
    ISwiftlyCore core,
    ZombieCatalogService catalog,
    Func<ILocalizationApi> localization) : IAbilityFactory
{
    public IAbility Create<T>() where T : IAbility => CreateByName(typeof(T) == typeof(DoubleJump)
        ? "double_jump" : typeof(T).Name.ToLowerInvariant());

    public IAbility CreateByName(string abilityName)
    {
        var definition = catalog.Current.Document.Abilities.FirstOrDefault(item => item.InternalName == abilityName)
            ?? throw new NotSupportedException($"Способность {abilityName} отсутствует в каталоге");
        return Create(definition);
    }

    internal IAbility Create(ZombieAbilityDefinition definition) => AbilityParameters.Parse(definition) switch
    {
        HealConfig config => new Heal(core, config, localization),
        LeapConfig config => new Leap(core, config, localization),
        BlindConfig config => new Blind(core, config),
        ChargeConfig config => new Charge(core, config, localization),
        TrapConfig config => new Trap(core, config, localization),
        CatchConfig config => new Catch(core, config, localization),
        DoubleJumpConfig config => new DoubleJump(core, config),
        _ => throw new NotSupportedException($"Неизвестная механика {definition.Kind}")
    };

    public List<IAbility> CreateFromStrings(List<string> abilities, ulong steamId = 0, AbilitySide side = AbilitySide.Zombie)
    {
        var snapshot = catalog.Current;
        // Удалённая способность в старом human_class.json не должна ломать создание игрока
        return snapshot.ResolveAbilities(abilities, steamId, side).Select(Create).ToList();
    }
}
