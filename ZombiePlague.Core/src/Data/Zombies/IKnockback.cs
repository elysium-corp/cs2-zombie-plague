using SwiftlyS2.Shared.GameEventDefinitions;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Core.Data.Zombies;

public interface IKnockback
{
    bool TryApplyKnockback(EventPlayerHurt @event, KnockbackData? knockbackData);

    void Start();
}