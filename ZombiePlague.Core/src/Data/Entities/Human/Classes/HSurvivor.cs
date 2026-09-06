using ZombiePlague.Core.Config.Human;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Entities.Human.Classes;

internal sealed class HSurvivor(IHClassConfig config, List<IAbility> abilities) : HMercenary(config, abilities);
