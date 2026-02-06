using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;

namespace CS2ZombiePlague.Data.Events;

public class EventDelegates
{
    public delegate void OnPlayerInfectedBy(IPlayer infector, IPlayer victim);  
    public delegate void OnWeaponDrop(IPlayer player, CCSWeaponBase weapon);  
}