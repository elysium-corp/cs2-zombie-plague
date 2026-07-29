using Microsoft.Extensions.Options;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZPCore.Config.Zombie;

namespace ZombiePlague.Core.Data.Zombies.ZClasses;

internal sealed class ZClassFactory(IOptions<ZClassConfig> config, IAbilityFactory abilityFactory) : IZClassFactory
{
    public IZClass Create<TClass>() where TClass : IZClass
    {
        return typeof(TClass) switch
        {
            var t when t == typeof(ZCleric) => new ZCleric(config.Value.Cleric, abilityFactory),
            var t when t == typeof(ZHunter) => new ZHunter(config.Value.Hunter, abilityFactory),
            var t when t == typeof(ZAssassin) => new ZAssassin(config.Value.Assassin, abilityFactory),
            var t when t == typeof(ZHeavy) => new ZHeavy(config.Value.Heavy, abilityFactory),
            var t when t == typeof(ZSmoker) => new ZSmoker(config.Value.Smoker, abilityFactory),
            var t when t == typeof(ZNemesis) => new ZNemesis(config.Value.Nemesis, abilityFactory),
            _ => throw new NotSupportedException("ZClassFactory: type TClass hasn't supported!") 
        };
    }
}