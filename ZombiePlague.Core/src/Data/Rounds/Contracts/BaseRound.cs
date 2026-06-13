using SwiftlyS2.Shared;
using ZombiePlague.Api.Data;
using ZombiePlague.Core.Data.Managers;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class BaseRound(ISwiftlyCore core, RoundManager roundManager, ZombieManager zombieManager)
    : IRound
{
    protected readonly ISwiftlyCore Core = core;
    protected readonly RoundManager RoundManager = roundManager;
    protected readonly ZombieManager ZombieManager = zombieManager;

    public abstract int Chance { get; }
    public abstract string Name { get; }

    public void Start() => OnStart();

    public virtual void End()
    {
        OnEnd();
        RoundManager.SetRound(new None());
        Core.PlayerManager.SendCenter("Раунд окончен");
    }

    protected virtual void OnEnd()
    {
    }

    protected virtual void OnStart()
    {
    }
}