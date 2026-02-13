namespace CS2ZombiePlague.Config.Weapon;

public interface IKnifeConfig
{
    public string InternalName { get; set; }
    public string DisplayName { get; set; }
    public string Model { get; set; }
    public string Description { get; set; }
    public float Speed { get; set; }
    public float Knockback { get; set; }
    public int Gravity { get; set; }
    public float DamageMultiplier { get; set; }
}