using ZombiePlague.Api.Data;

namespace CustomKnife.Data.Configs;

public sealed class KnifeConfig
{
    public MonarchKnifeConfig Knockback { get; set; } = new();
    public AncientConfig Speed { get; set; } = new();
    public GaiasVengeanceConfig Gravity { get; set; } = new();
    public KatanaConfig Vip { get; set; } = new();
}

public interface IKnifeConfig
{
    public byte Index { get; set; }
    public string DisplayName { get; set; }
    public string Model { get; set; }
    public string Description { get; set; }
    public float Speed { get; set; }
    public KnockbackData KnockbackData { get; set; }
    public int Gravity { get; set; }
    public float DamageMultiplier { get; set; }
}

public class MonarchKnifeConfig : IKnifeConfig
{
    public byte Index { get; set; } = 1;
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
    public byte Index { get; set; } = 2;
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
    public byte Index { get; set; } = 0;
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
    public byte Index { get; set; } = 3;
    public string DisplayName { get; set; } = "Katana";
    public string Model { get; set; } = "weapons/nozb1/valogun/knife/oni_katana_tactical/oni_katana_tactical_ag2.vmdl";
    public string Description { get; set; } = "VIP";
    public float Speed { get; set; } = 300f;
    public KnockbackData KnockbackData { get; set; } = new(1400, 150);
    public int Gravity { get; set; } = 550;
    public float DamageMultiplier { get; set; } = 3.0f;
}