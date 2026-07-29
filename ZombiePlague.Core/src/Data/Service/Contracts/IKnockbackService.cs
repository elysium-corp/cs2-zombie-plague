using SwiftlyS2.Shared.GameEventDefinitions;
using ZombiePlague.Api.Data;

namespace ZombiePlague.Core.Data.Service.Contracts;

internal interface IKnockbackService : IService
{
    bool TryApplyKnockback(
        EventPlayerHurt @event,
        KnockbackData? knockbackData = null
    );
}