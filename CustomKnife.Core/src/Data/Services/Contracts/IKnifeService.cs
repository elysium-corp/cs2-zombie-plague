using CustomKnife.Data.Models;
using SwiftlyS2.Shared.GameEventDefinitions;
using SwiftlyS2.Shared.GameHooks;
using SwiftlyS2.Shared.Players;

namespace CustomKnife.Data.Services.Contracts;

public interface IKnifeService
{
    bool TryGiveKnife(IPlayer player);

    bool TryApplyProperties(IPlayer? player);

    bool TryApplyKnifeDamage(ref TakeDamageEntityPreContext @event);

    bool TryApplyKnifeKnockback(EventPlayerHurt @event);

    IKnife GetKnife(IPlayer player);

    IReadOnlyCollection<IKnife> GetRegisteredKnives();

    Task SelectKnifeAsync(IPlayer player, IKnife knife, CancellationToken cancellationToken = default);
}