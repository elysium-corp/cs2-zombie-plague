using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Data;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Data;
using ZombiePlague.Core.Data.Managers;
using ZombiePlague.Core.Data.Rounds;
using ZombiePlague.Core.Data.Zombies;
using ZombiePlague.Core.Utils.Extensions;

namespace ZombiePlague.Core.Api;

public sealed class ZombiePlagueApi(
    IEventSubscriber eventSubscriber,
    IZombieManager zombieManager,
    IHumanManager humanManager,
    IKnockback knockback
    ) : IZombiePlagueApi
{
    public IEventSubscriber EventSubscriber => eventSubscriber;

    public bool IsInfected(IPlayer player) => player.IsInfected();
    
    public bool IsSurvivor(IPlayer player)
    {
        return humanManager.IsSurvivor(player);
    }

    public bool IsNemesis(IPlayer player)
    {
        return zombieManager.IsNemesis(player);
    }

    public bool IsNemesisRound(IRound round) => round is Nemesis;

    public bool IsPlagueRound(IRound round) => round is Plague;

    public bool IsArmageddonRound(IRound round) => round is Armageddon;

    public bool IsSurvivorRound(IRound round) => round is Survivor;
    
    public bool IsInfectionRound(IRound round) => round is Infection;
    
    public void ApplyKnockBack(EventPlayerHurt @event, KnockbackData data)
    {
        knockback.TryApplyKnockback(@event, data);
    }

    public bool IsNoneRound(IRound round) => round is None;
}