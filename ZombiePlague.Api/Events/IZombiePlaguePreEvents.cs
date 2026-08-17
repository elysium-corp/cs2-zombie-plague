using Common.Hooks;
using ZombiePlague.Api.Events.Contexts;

namespace ZombiePlague.Api.Events;

public interface IZombiePlaguePreEvents
{
    event HookHandler<PlayerInfectPreContext> PlayerInfectEvent;
}