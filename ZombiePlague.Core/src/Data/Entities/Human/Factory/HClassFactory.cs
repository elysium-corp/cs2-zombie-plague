using Microsoft.Extensions.Options;
using ZombiePlague.Core.Config.Human;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZombiePlague.Core.Data.Entities.Human.Classes;

namespace ZombiePlague.Core.Data.Entities.Human.Factory;

internal sealed class HClassFactory(IOptions<HClassConfig> config, IAbilityFactory abilityFactory) : IHClassFactory
{
    public IHClass Create<TClass>(ulong steamId = 0) where TClass : IHClass
    {
        return typeof(TClass) switch
        {
            var t when t == typeof(HMercenary) => new HMercenary(config.Value.Mercenary, abilityFactory, steamId),
            var t when t == typeof(HSurvivor) => new HSurvivor(config.Value.Survivor, abilityFactory, steamId),
            _ => throw new NotSupportedException("HClassFactory: type TClass hasn't supported!")
        };
    }

    public IHClass CreateOrDefault(string classId, ulong steamId = 0)
    {
        var classes = config.Value;

        return classId switch
        {
            _ when classId == classes.Mercenary.InternalName && classes.Mercenary.Enabled => Create<HMercenary>(steamId),
            _ when classId == classes.Survivor.InternalName && classes.Survivor.Enabled => Create<HSurvivor>(steamId),
            _ => Create<HMercenary>(steamId),
        };
    }
}