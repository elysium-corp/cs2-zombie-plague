namespace CS2ZombiePlague.Config;

public sealed class RoundConfig
{
    public InfectionConfig Infection { get; set; } = new();
    public PlagueConfig Plague { get; set; } = new();
    public NemesisConfig Nemesis { get; set; } = new();
    public SurvivorConfig Survivor { get; set; } = new();

    public List<IRoundConfig> Rounds =
        [new InfectionConfig(), new PlagueConfig(), new NemesisConfig(), new SurvivorConfig()];
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

public sealed class NemesisConfig : IRoundConfig
{
    public bool Enable { get; set; } = true;

    public int Chance { get; set; } = 1;
    
    public string MusicSoundName {get; set;} = "ZombiePlagueAbility.Nemesis";

    public bool NemesisLeap { get; set; } = true;
    public int NemesisBonusHealthPerPlayer { get; set; } = 1500;
}

public sealed class SurvivorConfig : IRoundConfig
{
    public bool Enable { get; set; } = true;

    public int Chance { get; set; } = 1;
    
    public string MusicSoundName {get; set;} = "ZombiePlagueAbility.Survivor";

    public int SurvivorBonusHealthPerZombie { get; set; } = 150;
}