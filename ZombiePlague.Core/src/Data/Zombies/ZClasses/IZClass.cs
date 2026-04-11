using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Zombies.ZClasses;

public interface IZClass
{
    public string InternalName { get; set; }
    
    public string DisplayName { get; set; }
    
    public string Model { get; set; }
    
    public string Description { get; set; }
    
    public int Health { get; set; }
    
    public float Speed { get; set; }
    
    public float Knockback { get; set; }
    
    public int Gravity { get; set; }

    public List<string> HurtSounds { get; set; }
    
    public List<IAbility> Abilities { get; set; }
}