using CS2ZombiePlague.Config.Zombie;
using CS2ZombiePlague.Data.Abilities;
using CS2ZombiePlague.Data.Abilities.Contracts;

namespace CS2ZombiePlague.Data.ZClasses;

public sealed class ZHeavy(ZombieHeavy config, IAbilityFactory abilityFactory) : IZClass
{
    public string InternalName { get; set; } = config.InternalName;

    public string DisplayName { get; set; } = config.DisplayName;

    public string Model { get; set; } = config.Model;

    public string ArmsModel { get; set; } = config.ArmsModel;

    public string Description { get; set; } = config.Description;

    public int Health { get; set; } = config.Health;

    public float Speed { get; set; } = config.Speed;

    public float Knockback { get; set; } = config.Knockback;

    public int Gravity { get; set; } = config.Gravity;

    public List<IAbility> Abilities { get; set; } = [abilityFactory.Create<Blind>()];
}