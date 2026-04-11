using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Api.Data;
using ZombiePlague.Api.Events;
using ZombiePlague.Api.Generated;

namespace ZombiePlague.Api;

public interface IZombiePlagueApi
{
    public IEventSubscriber EventSubscriber { get; }

    public bool IsInfected(IPlayer player);
    
    public bool IsSurvivor(IPlayer player);
    
    public bool IsNemesis(IPlayer player);
    
    public bool IsNoneRound(IRound round);
    
    public bool IsNemesisRound(IRound round);
    
    public bool IsPlagueRound(IRound round);
    
    public bool IsArmageddonRound(IRound round);
    
    public bool IsSurvivorRound(IRound round);
    
    public bool IsInfectionRound(IRound round);

    public void ApplyKnockBack(EventPlayerHurt @event, KnockbackData data);

    public static readonly string VersionApi = BuildInfo.ApiVersion;

    public static readonly string SharedApiKey = "ZombiePlague.Api.IZServiceApi";
}