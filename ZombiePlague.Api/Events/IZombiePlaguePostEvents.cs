using Common.Hooks;
using ZombiePlague.Api.Events.Contexts;

namespace ZombiePlague.Api.Events;

public interface IZombiePlaguePostEvents
{
    event HookHandler<PlayerInfectPostContext> PlayerInfectEvent;
}