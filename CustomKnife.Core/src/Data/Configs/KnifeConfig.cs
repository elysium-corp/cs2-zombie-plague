using ZombiePlague.Api.Data;

namespace CustomKnife.Data.Configs;

public sealed class KnifeConfig
{
    public MonarchConfig MonarchConfig { get; } = new();
    
    public AncientConfig AncientConfig { get; } = new();
    
    public GaiasVengeanceConfig GaiasVengeanceConfig { get; } = new();
    
    public KatanaConfig KatanaConfig { get; } = new();
}

public interface IKnifeConfig
{
    public bool Enabled { get; set; }
    public string InternalName { get; set; }
    public string DisplayName { get; set; }
    public string Model { get; set; }
    public string Description { get; set; }
    public float Speed { get; set; }
    public KnockbackData KnockbackData { get; set; }
    public int Gravity { get; set; }
    public float DamageMultiplier { get; set; }
}

public class MonarchConfig : IKnifeConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "knife_monarch";
    public string DisplayName { get; set; } = "Monarch";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/sovereign_tactical/sovereign_tactical_ag2.vmdl";
    public string Description { get; set; } = "Отдача";
    public float Speed { get; set; } = 250f;
    public KnockbackData KnockbackData { get; set; } = new(1400, 150);
    public int Gravity { get; set; } = 800;
    public float DamageMultiplier { get; set; } = 1.0f;
}

public class AncientConfig : IKnifeConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "knife_ancient";
    public string DisplayName { get; set; } = "Ancient";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/ejderbicak_cord/ejderbicak_cord_ag2.vmdl";
    public string Description { get; set; } = "Скорость";
    public float Speed { get; set; } = 300f;
    public KnockbackData KnockbackData { get; set; } = new(250, 150);
    public int Gravity { get; set; } = 800;
    public float DamageMultiplier { get; set; } = 1.0f;
}

public class GaiasVengeanceConfig : IKnifeConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "knife_vengeance";
    public string DisplayName { get; set; } = "Vengeance";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/ashen_kukri/ashen_kukri_ag2.vmdl";
    public string Description { get; set; } = "Гравитация";
    public float Speed { get; set; } = 250f;
    public KnockbackData KnockbackData { get; set; } = new(250, 150);
    public int Gravity { get; set; } = 600;
    public float DamageMultiplier { get; set; } = 1.0f;
}

public class KatanaConfig : IKnifeConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "knife_katana";
    public string DisplayName { get; set; } = "Katana";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/oni_katana_tactical/oni_katana_tactical_ag2.vmdl";
    public string Description { get; set; } = "VIP";
    public float Speed { get; set; } = 300f;
    public KnockbackData KnockbackData { get; set; } = new(1400, 150);
    public int Gravity { get; set; } = 550;
    public float DamageMultiplier { get; set; } = 3.0f;
}