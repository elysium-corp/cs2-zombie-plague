using ZombiePlague.Core.Data.Abilities;
using ZombiePlague.Core.Data.Abilities.Contracts;
using ZPCore.Config.Zombie;

namespace ZombiePlague.Core.Data.Zombies.ZClasses;

internal sealed class ZHeavy(ZombieHeavy config, IAbilityFactory abilityFactory) : IZClass
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

    public List<IAbility> Abilities { get; set; } = [abilityFactory.Create<Blind>(), abilityFactory.Create<Leap>()];
}