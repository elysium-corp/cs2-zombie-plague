using Common.Di;
using SwiftlyS2.Shared;
using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Entities.Human;

public interface IHClass
{ 
    public string Model { get; set; }
    
    public int Health { get; set; }
    
    public int Armor { get; set; }
    
    public float Speed { get; set; }
    
    public int Gravity { get; set; }
    
    public List<IAbility> Abilities { get; set; }
}