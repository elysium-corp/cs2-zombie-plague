using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Events;

public interface IEventPublisher
{
    void OnPlayerInfectedBy(IPlayer infector, IPlayer victim);
    void OnPlayerInfected(IPlayer victim);
    void OnWeaponDrop(IPlayer player, CCSWeaponBase weapon);
}