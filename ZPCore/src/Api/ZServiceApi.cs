using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;
using ZPApi;
using ZPApi.Data;
using ZPApi.Events;
using ZPCore.Data;
using ZPCore.Data.Extensions;
using ZPCore.Data.Managers;
using ZPCore.Data.Rounds;
using ZPCore.Di;

namespace ZPCore.Api;

public sealed class ZServiceApi(IEventSubscriber eventSubscriber) : IZServiceApi
{
    public IEventSubscriber EventSubscriber => eventSubscriber;

    public bool IsInfected(IPlayer player) => player.IsInfected();
    
    public bool IsSurvivor(IPlayer player)
    {
        var humanManager =  DependencyManager.GetService<HumanManager>();
        return humanManager.IsSurvivor(player);
    }

    public bool IsNemesis(IPlayer player)
    {
        var zombieManager =  DependencyManager.GetService<ZombieManager>();
        return zombieManager.IsNemesis(player);
    }

    public bool IsNemesisRound(IRound round) => round is Nemesis;

    public bool IsPlagueRound(IRound round) => round is Plague;

    public bool IsArmageddonRound(IRound round) => round is Armageddon;

    public bool IsSurvivorRound(IRound round) => round is Survivor;
    
    public bool IsInfectionRound(IRound round) => round is Infection;
    
    public void ApplyKnockBack(EventPlayerHurt @event, KnockbackData data)
    {
        var knockbackSystem = DependencyManager.GetService<Knockback>();
        knockbackSystem.TryApplyKnockback(@event, data);
    }

    public bool IsNoneRound(IRound round) => round is None;
}