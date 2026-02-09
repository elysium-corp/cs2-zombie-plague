namespace CS2ZombiePlague.Config.Zombie;

public interface IZClassConfig
{
    public bool Enabled { get; set; }
    public string InternalName { get; set; }
    public string DisplayName { get; set; }
    public string Model { get; set; }
    public string Description { get; set; }
    public int Health { get; set; }
    public float Speed { get; set; }
    public float Knockback { get; set; }
    public int Gravity { get; set; }
    public List<string> Abilities { get; set; }
}