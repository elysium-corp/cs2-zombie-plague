using ZombiePlague.Api.Data;
using ZPCore.Config.Round;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal interface IRoundFactory
{
    IRound Create(IRoundConfig? config);
}
