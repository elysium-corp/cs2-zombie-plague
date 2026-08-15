using ZombiePlague.Core.Config.Zombie;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Entities.Zombie.Classes;

internal sealed class ZCleric(ZombieCleric config, IAbilityFactory abilityFactory) : IZClass
{
    public string InternalName { get; set; } = config.InternalName;

    public string DisplayName { get; set; } = config.DisplayName;

    public string Model { get; set; } = config.Model;

    public string Description { get; set; } = config.Description;

    public int Health { get; set; } = config.Health;

    public float Speed { get; set; } = config.Speed;

    public float Knockback { get; set; } = config.Knockback;

    public int Gravity { get; set; } = config.Gravity;
    
    public string InfectionSound { get; set; } = config.InfectionSound;
    
    public List<string> HurtSounds { get; set; } = config.HurtSounds;

    public List<IAbility> Abilities { get; set; } = abilityFactory.CreateFromStrings(config.Abilities);
}