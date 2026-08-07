using Microsoft.Extensions.Options;
using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Entities.Zombie.Classes;

namespace ZombiePlague.Core.Data.Entities.Zombie.Factory;

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

    public IZClass CreateOrDefault(string classId)
    {
        var classes = config.Value;

        return classId switch
        {
            _ when classId == classes.Cleric.InternalName && classes.Cleric.Enabled => Create<ZCleric>(),
            _ when classId == classes.Hunter.InternalName && classes.Hunter.Enabled => Create<ZHunter>(),
            _ when classId == classes.Assassin.InternalName && classes.Assassin.Enabled => Create<ZAssassin>(),
            _ when classId == classes.Heavy.InternalName && classes.Heavy.Enabled => Create<ZHeavy>(),
            _ when classId == classes.Smoker.InternalName && classes.Smoker.Enabled => Create<ZSmoker>(),
            _ when classId == classes.Nemesis.InternalName && classes.Nemesis.Enabled => Create<ZNemesis>(),
            _ => Create<ZCleric>()
        };
    }
}