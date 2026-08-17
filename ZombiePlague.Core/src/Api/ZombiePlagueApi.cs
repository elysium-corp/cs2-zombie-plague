using Common.Hooks.Abstractions;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api;
using ZombiePlague.Api.Data;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Api.Events;
using ZombiePlague.Core.Api.Events;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Rounds;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Api;

internal sealed class ZombiePlagueApi(
    ZombiePlagueEvents events,
    IPlayerManager playerManager,
    IKnockbackService knockbackService,
    IPlayerRepository playerRepository
) : IZombiePlagueApi
{
    public IZombiePlagueEvents Events => events;
    
    public IPlayerRepository PlayerRepository => playerRepository;

    public bool IsInfected(IPlayer player)
    {
        return player.IsValid && playerManager.IsZombie(player);
    }
    
    public bool IsSurvivor(IPlayer player)
    {
        return player.IsValid && playerManager.IsSurvivor(player);
    }

    public bool IsNemesis(IPlayer player)
    {
        return player.IsValid && playerManager.IsNemesis(player);
    }

    public bool IsNemesisRound(IRound round) => false;

    public bool IsPlagueRound(IRound round) => false;

    public bool IsArmageddonRound(IRound round) => false;

    public bool IsSurvivorRound(IRound round) => false;

    public bool IsInfectionRound(IRound round) => round is Infection;

    public bool IsNoneRound(IRound round) => false;
    
    public void ApplyKnockBack(EventPlayerHurt @event, KnockbackData data)
    {
        knockbackService.TryApplyKnockback(@event, data);
    }
}