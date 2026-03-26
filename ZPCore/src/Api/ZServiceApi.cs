using SwiftlyS2.Shared.Players;
using ZPApi;
using ZPApi.Data;
using ZPApi.Events;
using ZPCore.Data.Extensions;
using ZPCore.Data.Rounds;

namespace ZPCore.Api;

public sealed class ZServiceApi(IEventSubscriber eventSubscriber) : IZServiceApi
{
    public IEventSubscriber EventSubscriber => eventSubscriber;

    public bool IsInfected(IPlayer player) => player.IsInfected();

    public bool IsNemesisRound(IRound round) => round is Nemesis;

    public bool IsPlagueRound(IRound round) => round is Plague;

    public bool IsArmageddonRound(IRound round) => round is Armageddon;

    public bool IsSurvivorRound(IRound round) => round is Survivor;
    
    public bool IsInfectionRound(IRound round) => round is Infection;

    public bool IsNoneRound(IRound round) => round is None;
}