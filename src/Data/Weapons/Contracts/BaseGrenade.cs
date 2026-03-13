using SwiftlyS2.Shared.Natives;
using SwiftlyS2.Shared.Players;

namespace CS2ZombiePlague.Data.Weapons.Contracts;

public abstract class BaseGrenade : BaseWeapon
{
    public virtual string OnDecoyStartedParticleName => "";
    
    public virtual void OnDecoyStarted(Vector position) { }
    
    public virtual void OnHegrenadeDetonate(Vector position) { }
    
    public virtual void OnMolotovDetonate(IPlayer attacker, Vector position) { }
    
    public virtual void OnSmokegrenadeDetonate(Vector position) { }
}