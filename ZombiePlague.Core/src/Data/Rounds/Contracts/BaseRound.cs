using SwiftlyS2.Shared;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Core.Data.Rounds.Contracts;

internal abstract class BaseRound(ISwiftlyCore core) : IRound
{
    protected readonly ISwiftlyCore Core = core;

    public abstract int Chance { get; }
    public abstract string Name { get; }

    public void Start() => OnStart();

    public virtual void End()
    {
        OnEnd();
        Core.PlayerManager.SendCenter("Раунд окончен");
    }

    protected virtual void OnEnd()
    {
    }

    protected virtual void OnStart()
    {
    }
}
