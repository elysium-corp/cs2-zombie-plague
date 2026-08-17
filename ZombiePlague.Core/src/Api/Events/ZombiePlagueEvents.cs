using ZombiePlague.Api.Events;

namespace ZombiePlague.Core.Api.Events;

internal sealed class ZombiePlagueEvents(ZombiePlaguePreEvents pre, ZombiePlaguePostEvents post) : IZombiePlagueEvents
{
    public IZombiePlaguePreEvents Pre => pre;

    public IZombiePlaguePostEvents Post => post;
}