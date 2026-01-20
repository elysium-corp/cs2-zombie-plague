using CS2ZombiePlague.Config.Weapon;

namespace CS2ZombiePlague.Data.Weapons.Knifes;

public class VipKnifeWeapon(IKnifeConfig config) : IKnife
{
    public string InternalName { get; set; } = config.InternalName;
    public string DisplayName { get; set; } = config.DisplayName;
    public string Model { get; set; } = config.Model;
    public string Description { get; set; } = config.Description;
    public float Speed { get; set; } = config.Speed;
    public float Knockback { get; set; } = config.Knockback;
    public int Gravity { get; set; } = config.Gravity;
}