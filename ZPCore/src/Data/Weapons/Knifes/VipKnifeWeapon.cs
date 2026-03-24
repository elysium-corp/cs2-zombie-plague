using ZPCore.Config.Weapon;

namespace ZPCore.Data.Weapons.Knifes;

internal class VipKnifeWeapon(IKnifeConfig config) : IKnife
{
    public string InternalName { get; set; } = config.InternalName;
    public string DisplayName { get; set; } = config.DisplayName;
    public string Model { get; set; } = config.Model;
    public string Description { get; set; } = config.Description;
    public float Speed { get; set; } = config.Speed;
    public float Knockback { get; set; } = config.Knockback;
    public int Gravity { get; set; } = config.Gravity;
    public float DamageMultiplier { get; set; } = config.DamageMultiplier;
}