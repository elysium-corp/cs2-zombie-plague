using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Entities.Zombie.Classes;

internal sealed class ZNemesis(IZClassConfig config, List<IAbility> abilities) : ZCatalogClass(config, abilities);
