namespace ZPCore.Config.Round;

public sealed class RoundConfig
{
    // Регистрация раундов.
    public InfectionConfig Infection { get; set; } = new();
    public PlagueConfig Plague { get; set; } = new();
    public NemesisConfig Nemesis { get; set; } = new();
    public SurvivorConfig Survivor { get; set; } = new();
    public ArmageddonConfig Armageddon { get; set; } = new();
}

public sealed class InfectionConfig : IRoundConfig
{
    // Включить раунд инфекции.
    public bool Enable { get; set; } = true;
    
    // Вероятность выпадения раунда.
    public int Chance { get; set; } = 20;
    
    // Включить возрождение зомби в течении раунда.
    public bool ZombieRevived { get; set; } = true;
    
    // Включен ли рывок у первого зомби.
    public bool FirstZombieLeap { get; set; } = true;
    
    // Коэффициент стартового здоровья первого зомби.
    public float FirstZombieHealthRatio { get; set; } = 1.0f;
    
    // Время возрождения зомби.
    public float ZombieSpawnTime { get; set; } = 5.0f;
}

public sealed class PlagueConfig : IRoundConfig
{
    // Включить раунд массового заражения.
    public bool Enable { get; set; } = true;
    
    // Вероятность выпадения раунда.
    public int Chance { get; set; } = 4;
    
    // Включить возрождение зомби в течении раунда.
    public bool ZombieRevived { get; set; } = true;
    
    // Процент игроков, которые будут заражены в начале раунда.
    public float ZombieSpawnRatio { get; set; } = 0.3f;
    
    // Время возрождения зомби.
    public float ZombieSpawnTime { get; set; } = 5.0f;
}

public interface INemesisConfig
{
    // Количество дополнительного здоровья немезиды, за каждого игрока.
    public int NemesisBonusHealthPerPlayer { get; set; }
}

public interface ISurvivorConfig
{
    // Модель выжившего.
    public string SurvivorModel { get; set; }
    
    // Количество дополнительного здоровья выжившего, за каждого игрока.
    public int SurvivorBonusHealthPerZombie { get; set; }
}

public sealed class NemesisConfig : IRoundConfig, INemesisConfig
{
    // Включить раунд массового заражения.
    public bool Enable { get; set; } = true;
    
    // Включить музыку, которая будет проигрываться в начале раунда.
    public bool IsMusicEnabled { get; set; } = true;
    
    // Вероятность выпадения раунда.
    public int Chance { get; set; } = 1;
    
    // Название музыкального ивента.
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Nemesis";
    
    // Включить рывок у немезиды.
    public bool NemesisLeap { get; set; } = true;
    
    // Количество дополнительного здоровья выжившего, за каждого игрока.
    public int NemesisBonusHealthPerPlayer { get; set; } = 1500;
    
    // Дополнительный урон, который наносит немезида.
    public int NemesisExtraDamage  { get; set; } = 125;
}

public sealed class SurvivorConfig : IRoundConfig, ISurvivorConfig
{
    // Включить раунд выжившего.
    public bool Enable { get; set; } = true;
    
    // Включить музыку, которая будет проигрываться в начале раунда.
    public bool IsMusicEnabled { get; set; } = true;
    
    // Вероятность выпадения раунда.
    public int Chance { get; set; } = 1;
    
    // Название музыкального ивента.
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Survivor";
    
    // Модель выжившего.
    public string SurvivorModel { get; set; } = "characters/models/nozb1/nanosuit_player_model/nanosuit_player_model.vmdl";
    
    // Количество дополнительного здоровья выжившего, за каждого игрока.
    public int SurvivorBonusHealthPerZombie { get; set; } = 150;
}

public sealed class ArmageddonConfig : IRoundConfig, ISurvivorConfig, INemesisConfig
{
    // Включить раунд армагеддон.
    public bool Enable { get; set; } = true;
    
    // Включить музыку, которая будет проигрываться в начале раунда.
    public bool IsMusicEnabled { get; set; } = true;
    
    // Вероятность выпадения раунда.
    public int Chance { get; set; } = 2;
    
    // Количество дополнительного здоровья немезиды, за каждого игрока.
    public int NemesisBonusHealthPerPlayer { get; set; } = 1000;
    
    // Название музыкального ивента.
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Armageddon";

    // Модель выжившего.
    public string SurvivorModel { get; set; } = "characters/models/nozb1/nanosuit_player_model/nanosuit_player_model.vmdl";

    // Количество дополнительного здоровья выжившего, за каждого игрока.
    public int SurvivorBonusHealthPerZombie { get; set; } = 100;
    
    // Дополнительный урон, который наносит немезида.
    public int NemesisExtraDamage  { get; set; } = 125;
}