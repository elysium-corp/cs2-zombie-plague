using CS2ZombiePlague.Config.Zombie;
using CS2ZombiePlague.Data.Abilities;
using CS2ZombiePlague.Data.Abilities.Contracts;

namespace CS2ZombiePlague.Data.Zombies.ZClasses;

public sealed class ZSmoker(ZombieSmoker config, IAbilityFactory abilityFactory) : IZClass
{
    public string InternalName { get; set; } = config.InternalName;

    public string DisplayName { get; set; } = config.DisplayName;

    public string Model { get; set; } = config.Model;

    public string Description { get; set; } = config.Description;

    public int Health { get; set; } = config.Health;

    public float Speed { get; set; } = config.Speed;

    public float Knockback { get; set; } = config.Knockback;

    public int Gravity { get; set; } = config.Gravity;
    
    public List<string> HurtSounds { get; set; } = config.HurtSounds;

    public List<IAbility> Abilities { get; set; } = [abilityFactory.Create<Catch>()];
}