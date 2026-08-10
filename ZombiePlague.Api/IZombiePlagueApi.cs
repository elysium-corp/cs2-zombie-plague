using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data;
using ZombiePlague.Api.Data.Store;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Generated;

namespace ZombiePlague.Api;

public interface IZombiePlagueApi
{
    public IEventSubscriber EventSubscriber { get; }
    
    public IPlayerRepository PlayerRepository { get; }

    public bool IsInfected(IPlayer player);

    public bool IsNemesisRound(IRound round);
    
    public bool IsSurvivorRound(IRound round);
    
    public void ApplyKnockBack(EventPlayerHurt @event, KnockbackData data);

    public static readonly string VersionApi = BuildInfo.ApiVersion;

    public static readonly string SharedApiKey = "ZombiePlague.Api.IZServiceApi";
}