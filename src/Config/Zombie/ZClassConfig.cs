namespace CS2ZombiePlague.Config.Zombie;

public sealed class ZClassConfig
{
    public ZombieCleric Cleric { get; set; } = new();
    public ZombieHunter Hunter { get; set; } = new();
    public ZombieAssassin Assassin { get; set; } = new();
    public ZombieHeavy Heavy { get; set; } = new();
    public ZombieSmoker Smoker { get; set; } = new();
    public ZombieNemesis Nemesis { get; set; } = new();
}

public class ZombieCleric : IZClassConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "zombie_cleric";
    public string DisplayName { get; set; } = "Cleric";
    public string Model { get; set; } = "characters/models/nozb1/zombie_stalker_player_model/zombie_stalker_player_model.vmdl";
    public string Description { get; set; } = "Лечит зомби";
    public int Health { get; set; } = 3_500;
    public float Speed { get; set; } = 260f;
    public float Knockback { get; set; } = 0.9f;
    public int Gravity { get; set; } = 600;
    public List<string> HurtSounds { get; set; } = ["ZombiePlagueSounds.zombie_hurt_1"];
    public List<string> Abilities { get; set; } = ["heal"];
}

public class ZombieHunter : IZClassConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "zombie_hunter";
    public string DisplayName { get; set; } = "Hunter";
    public string Model { get; set; } = "characters/models/kolka/2025/lurker/lurker.vmdl";
    public string Description { get; set; } = "Ставит ловушки";
    public int Health { get; set; } = 3_500;
    public float Speed { get; set; } = 260f;
    public float Knockback { get; set; } = 0.9f;
    public int Gravity { get; set; } = 600;
    public List<string> HurtSounds { get; set; } = ["ZombiePlagueSounds.zombie_hurt_1"];
    public List<string> Abilities { get; set; } = [];
}

public class ZombieAssassin : IZClassConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "zombie_assassin";
    public string DisplayName { get; set; } = "Assassin";
    public string Model { get; set; } = "characters/models/nozb1/zhunter_player_model/zhunter_player_model.vmdl";
    public string Description { get; set; } = "Ускоряется";
    public int Health { get; set; } = 3_200;
    public float Speed { get; set; } = 280f;
    public float Knockback { get; set; } = 0.9f;
    public int Gravity { get; set; } = 600;
    public List<string> HurtSounds { get; set; } = ["ZombiePlagueSounds.zombie_hurt_1"];
    public List<string> Abilities { get; set; } = [];
}

public class ZombieHeavy : IZClassConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "zombie_heavy";
    public string DisplayName { get; set; } = "Heavy";
    public string Model { get; set; } = "characters/models/nozb1/chris_walker_player_model/chris_walker_player_model.vmdl";
    public string Description { get; set; } = "Ослепляет";
    public int Health { get; set; } = 4_500;
    public float Speed { get; set; } = 250f;
    public float Knockback { get; set; } = 0.9f;
    public int Gravity { get; set; } = 700;
    public List<string> HurtSounds { get; set; } = ["ZombiePlagueSounds.zombie_hurt_1"];
    public List<string> Abilities { get; set; } = [];
}

public class ZombieSmoker : IZClassConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "zombie_smoker";
    public string DisplayName { get; set; } = "Smoker";
    public string Model { get; set; } = "characters/models/nozb1/jason_player_model/jason_player_model.vmdl";
    public string Description { get; set; } = "Притягивает";
    public int Health { get; set; } = 2_500;
    public float Speed { get; set; } = 250f;
    public float Knockback { get; set; } = 0.9f;
    public int Gravity { get; set; } = 800;
    public List<string> HurtSounds { get; set; } = ["ZombiePlagueSounds.zombie_hurt_1"];
    public List<string> Abilities { get; set; } = [];
}

public class ZombieNemesis : IZClassConfig
{
    public bool Enabled { get; set; } = true;
    public string InternalName { get; set; } = "zombie_nemesis";
    public string DisplayName { get; set; } = "Nemesis";
    public string Model { get; set; } = "characters/models/nozb1/nemesis_player_model/nemesis_player_model.vmdl";
    public string Description { get; set; } = "Убивает";
    public int Health { get; set; } = 5_000;
    public float Speed { get; set; } = 280f;
    public float Knockback { get; set; } = 1f;
    public int Gravity { get; set; } = 400;
    public List<string> HurtSounds { get; set; } = ["ZombiePlagueSounds.zombie_hurt_1"];
    public List<string> Abilities { get; set; } = [];
}