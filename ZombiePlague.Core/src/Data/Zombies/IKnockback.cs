using SwiftlyS2.Shared.GameEventDefinitions;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Core.Data.Zombies;

internal interface IKnockback
{
    bool TryApplyKnockback(EventPlayerHurt @event, KnockbackData? knockbackData);

    void Start();

    void Stop();
}
