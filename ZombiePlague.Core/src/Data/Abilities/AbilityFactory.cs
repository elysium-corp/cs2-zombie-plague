using Localization.Api;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Abilities;

internal class AbilityFactory(
    ISwiftlyCore core,
    IOptions<AbilityConfig> config,
    Func<ILocalizationApi> localization) : IAbilityFactory
{
    public IAbility Create<T>() where T : IAbility
    {
        return typeof(T) switch
        {
            var t when t == typeof(Heal) => new Heal(core, config.Value.Heal, localization),
            var t when t == typeof(Leap) => new Leap(core, config.Value.Leap, localization),
            var t when t == typeof(Blind) => new Blind(core, config.Value.Blind),
            var t when t == typeof(Charge) => new Charge(core, config.Value.Charge, localization),
            var t when t == typeof(Trap) => new Trap(core, config.Value.Trap, localization),
            var t when t == typeof(Catch) => new Catch(core, config.Value.Catch, localization),
            var t when t == typeof(DoubleJump) => new DoubleJump(core, config.Value.DoubleJump),
            _ => throw new NotSupportedException("ZAbilityFactory: type T hasn't supported!")
        };
    }

    public IAbility CreateByName(string abilityName)
    {
        return abilityName.ToLowerInvariant() switch
        {
            "heal" => Create<Heal>(),
            "leap" => Create<Leap>(),
            "blind" => Create<Blind>(),
            "charge" => Create<Charge>(),
            "trap" => Create<Trap>(),
            "catch" => Create<Catch>(),
            "double_jump" => Create<DoubleJump>(),
            
            _ => throw new NotSupportedException($"Ability '{abilityName}' is not supported.")
        };
    }

    public List<IAbility> CreateFromStrings(List<string> abilities)
    {
        return abilities
            .Select(CreateByName)
            .ToList();
    }
}
