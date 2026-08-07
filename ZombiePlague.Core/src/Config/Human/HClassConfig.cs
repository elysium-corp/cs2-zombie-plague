namespace ZombiePlague.Core.Config.Human;

public sealed class HClassConfig
{
    public HumanMercenary Mercenary { get; set; } = new();
    public HumanSurvivor Survivor { get; set; } = new();
}

public class HumanMercenary : IHClassConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "human_mercenary";
    public string DisplayName { get; set; } = "Mercenary";
    public string Model { get; set; } = "";
    public string Description { get; set; } = "Наемник";
    public int Health { get; set; } = 100;
    public int Armor { get; set; } = 0;
    public float Speed { get; set; } = 250f;
    public int Gravity { get; set; } = 800;
    public List<string> Abilities { get; set; } = [];
}

public class HumanSurvivor : IHClassConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "human_survivor";
    public string DisplayName { get; set; } = "Survivor";
    public string Model { get; set; } = "characters/models/nozb1/nanosuit_player_model/nanosuit_player_model.vmdl";
    public string Description { get; set; } = "Выживший";
    // - по умолчанию, число в раунде может меняться в зависимости от условий
    public int Health { get; set; } = 500;
    public int Armor { get; set; } = 0;
    public float Speed { get; set; } = 250f;
    public int Gravity { get; set; } = 800;
    public List<string> Abilities { get; set; } = ["leap"];
}