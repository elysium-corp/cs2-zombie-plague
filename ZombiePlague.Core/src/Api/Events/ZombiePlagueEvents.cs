using ZombiePlague.Api.Events;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlagueEvents(
    ZombiePlaguePlayerEvents players,
    ZombiePlagueClassEvents classes,
    ZombiePlagueRoundEvents rounds,
    ZombiePlagueCombatEvents combat
) : IZombiePlagueEvents
{
    public IZombiePlaguePlayerEvents Players => players;

    public IZombiePlagueClassEvents Classes => classes;

    public IZombiePlagueRoundEvents Rounds => rounds;

    public IZombiePlagueCombatEvents Combat => combat;
}
