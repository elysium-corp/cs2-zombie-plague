namespace CS2ZombiePlague.Config;

public sealed class RoundConfig
{
    public InfectionConfig Infection { get; set; } = new();
    public PlagueConfig Plague { get; set; } = new();
    public NemesisConfig Nemesis { get; set; } = new();
    public SurvivorConfig Survivor { get; set; } = new();
    public ArmageddonConfig Armageddon { get; set; } = new();
}

public sealed class InfectionConfig : IRoundConfig
{
    public bool Enable { get; set; } = true;
    public int Chance { get; set; } = 20;
    public bool ZombieRevived { get; set; } = true;
    public bool FirstZombieLeap { get; set; } = true;
    public float FirstZombieHealthRatio { get; set; } = 1.0f;
    public float ZombieSpawnTime { get; set; } = 5.0f;
}

public sealed class PlagueConfig : IRoundConfig
{
    public bool Enable { get; set; } = true;
    public int Chance { get; set; } = 4;
    public bool ZombieRevived { get; set; } = true;
    public float ZombieSpawnRatio { get; set; } = 0.3f;
    public float ZombieSpawnTime { get; set; } = 5.0f;
    public int InfectionChance { get; set; } = 10;
}

public interface INemesisConfig
{
    public int NemesisBonusHealthPerPlayer { get; set; }
}

public interface ISurvivorConfig
{
    public string Model { get; set; }
    public int SurvivorBonusHealthPerZombie { get; set; }
}

public sealed class NemesisConfig : IRoundConfig, INemesisConfig
{
    public bool Enable { get; set; } = true;
    public int Chance { get; set; } = 1;
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Nemesis";
    public bool NemesisLeap { get; set; } = true;
    public int NemesisBonusHealthPerPlayer { get; set; } = 1500;
}

public sealed class SurvivorConfig : IRoundConfig, ISurvivorConfig
{
    public bool Enable { get; set; } = true;
    public int Chance { get; set; } = 1;
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Survivor";
    public string Model { get; set; } = "characters/models/nozb1/nanosuit_player_model/nanosuit_player_model.vmdl";
    public int SurvivorBonusHealthPerZombie { get; set; } = 150;
}

public sealed class ArmageddonConfig : IRoundConfig, ISurvivorConfig, INemesisConfig
{
    public bool Enable { get; set; } = true;

    public int Chance { get; set; } = 2;
    
    public int NemesisBonusHealthPerPlayer { get; set; } = 1000;

    public string Model { get; set; } = "characters/models/nozb1/nanosuit_player_model/nanosuit_player_model.vmdl";

    public int SurvivorBonusHealthPerZombie { get; set; } = 100;
}