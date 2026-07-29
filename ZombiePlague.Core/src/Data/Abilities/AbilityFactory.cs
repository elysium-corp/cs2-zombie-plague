using Microsoft.Extensions.Options;
using SwiftlyS2.Shared;
using ZombiePlague.Core.Config.Ability;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Managers;

namespace ZombiePlague.Core.Data.Abilities;

internal class AbilityFactory(ISwiftlyCore core, IOptions<AbilityConfig> config, IZombieManager zombieManager) : IAbilityFactory
{
    public IAbility Create<T>() where T : IAbility
    {
        return typeof(T) switch
        {
            var t when t == typeof(Heal) => new Heal(core, config.Value.Heal, zombieManager),
            var t when t == typeof(Leap) => new Leap(core, config.Value.Leap),
            var t when t == typeof(Blind) => new Blind(core, config.Value.Blind),
            var t when t == typeof(Charge) => new Charge(core, config.Value.Charge, zombieManager),
            var t when t == typeof(Trap) => new Trap(core, config.Value.Trap),
            var t when t == typeof(Catch) => new Catch(core, config.Value.Catch),
            _ => throw new NotSupportedException("ZAbilityFactory: type T hasn't supported!")
        };
    }
}