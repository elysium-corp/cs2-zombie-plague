using CS2ZombiePlague.Config.Ability;
using CS2ZombiePlague.Data.Abilities.Contracts;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;

namespace CS2ZombiePlague.Data.Abilities;

public class AbilityFactory(ISwiftlyCore core, IOptions<AbilityConfig> config) : IAbilityFactory
{
    public IAbility Create<T>() where T : IAbility
    {
        return typeof(T) switch
        {
            var t when t == typeof(Heal) => new Heal(core, config.Value.Heal),
            var t when t == typeof(Leap) => new Leap(core, config.Value.Leap),
            var t when t == typeof(Blind) => new Blind(core, config.Value.Blind),
            var t when t == typeof(Charge) => new Charge(core, config.Value.Charge),
            _ => throw new NotSupportedException("ZAbilityFactory: type T hasn't supported!")
        };
    }
}