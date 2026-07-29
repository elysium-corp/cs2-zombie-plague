using ZombiePlague.Core.Config.Human;
using ZombiePlague.Core.Data.Abilities.Contracts;

namespace ZombiePlague.Core.Data.Entities.Human.Classes;

internal sealed class HSurvivor(HumanSurvivor config, IAbilityFactory abilityFactory) : IHClass
{
    public string Model { get; set; } = config.Model;

    public int Health { get; set; } = config.Health;

    public int Armor { get; set; } = config.Armor;

    public float Speed { get; set; } = config.Speed;

    public int Gravity { get; set; } = config.Gravity;

    public List<IAbility> Abilities { get; set; } = abilityFactory.CreateFromStrings(config.Abilities);
}