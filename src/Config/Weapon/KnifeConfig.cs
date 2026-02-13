namespace CS2ZombiePlague.Config.Weapon;

public sealed class KnifeConfig
{
    public KnockbackKnife Knockback { get; set; } = new();
    public SpeedKnife Speed { get; set; } = new();
    public GravityKnife Gravity { get; set; } = new();
    
    public VipKnife Vip { get; set; } = new();
}

public class KnockbackKnife () : IKnifeConfig
{
    public string InternalName { get; set; } = "knife_knockback";
    public string DisplayName { get; set; } = "Knockback Knife";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/sovereign_tactical/sovereign_tactical_ag2.vmdl";
    public string Description { get; set; } = "Отдача";
    public float Speed { get; set; } = 250f;
    public float Knockback { get; set; } = 1400f;
    public int Gravity { get; set; } = 800;
    public float DamageMultiplier { get; set; } = 1.0f;
}

public class SpeedKnife () : IKnifeConfig
{
    public string InternalName { get; set; } = "knife_speed";
    public string DisplayName { get; set; } = "Speed Knife";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/ejderbicak_cord/ejderbicak_cord_ag2.vmdl";
    public string Description { get; set; } = "Скорость";
    public float Speed { get; set; } = 300f;
    public float Knockback { get; set; } = 350f;
    public int Gravity { get; set; } = 800;
    public float DamageMultiplier { get; set; } = 1.0f;
}

public class GravityKnife () : IKnifeConfig
{
    public string InternalName { get; set; } = "knife_gravity";
    public string DisplayName { get; set; } = "Gravity Knife";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/ashen_kukri/ashen_kukri_ag2.vmdl";
    public string Description { get; set; } = "Гравитация";
    public float Speed { get; set; } = 250f;
    public float Knockback { get; set; } = 350f;
    public int Gravity { get; set; } = 600;
    public float DamageMultiplier { get; set; } = 1.0f;
}

public class VipKnife () : IKnifeConfig
{
    public string InternalName { get; set; } = "knife_vip";
    public string DisplayName { get; set; } = "Katana";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/oni_katana_tactical/oni_katana_tactical_ag2.vmdl";
    public string Description { get; set; } = "VIP";
    public float Speed { get; set; } = 300f;
    public float Knockback { get; set; } = 1400f;
    public int Gravity { get; set; } = 550;
    public float DamageMultiplier { get; set; } = 3.0f;
}