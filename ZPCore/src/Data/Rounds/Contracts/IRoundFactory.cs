using ZPCore.Config.Round;
using ZPCore.Data.Managers;
using ZPApi.Data;

namespace ZPCore.Data.Rounds.Contracts;

internal interface IRoundFactory
{
    public IRound Create(IRoundConfig? config, RoundManager roundManager);
}