using SwiftlyS2.Shared.Players;
using ZPApi.Data;

namespace ZPCore.Data.Effects.Contracts;

internal interface IEffectFactory
{
    public IEffect Create<T>(IPlayer? caster, IPlayer target) where T : IEffect;
}