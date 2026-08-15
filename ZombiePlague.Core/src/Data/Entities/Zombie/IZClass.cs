using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Entities.Zombie;

internal interface IZClass : IClass
{
    public string Model { get; set; }
    
    public int Health { get; set; }
    
    public float Speed { get; set; }
    
    public float Knockback { get; set; }
    
    public int Gravity { get; set; }
    
    public string InfectionSound { get; set; }
    
    public List<string> HurtSounds { get; set; }
    
    public List<IAbility> Abilities { get; set; }
}