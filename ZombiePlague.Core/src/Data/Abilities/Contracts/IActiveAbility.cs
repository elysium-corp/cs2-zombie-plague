using SwiftlyS2.Shared.Events;

namespace ZombiePlague.Core.Data.Abilities.Contracts;

internal interface IActiveAbility : IAbility
{
    public KeyKind? Key { get; }

    public void OnClientKeyStateChanged(IOnClientKeyStateChangedEvent @event);
}