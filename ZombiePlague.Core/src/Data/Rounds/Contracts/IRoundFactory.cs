using ZombiePlague.Api.Data;
using ZombiePlague.Core.Data.Managers;
using ZPCore.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal interface IRoundFactory
{
    public IRound Create(IRoundConfig? config, RoundManager roundManager);
}