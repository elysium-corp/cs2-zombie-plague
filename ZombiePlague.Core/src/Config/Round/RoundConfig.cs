namespace ZombiePlague.Core.Config.Round;

public sealed class RoundConfig
{
    public InfectionConfig Infection { get; set; } = new();
    public PlagueConfig Plague { get; set; } = new();
    public NemesisConfig Nemesis { get; set; } = new();
    public SurvivorConfig Survivor { get; set; } = new();
    
    public IEnumerable<IRoundConfig> GetAll()
    {
        return GetType()
            .GetProperties()
            .Select(property => property.GetValue(this))
            .OfType<IRoundConfig>();
    }
}

public sealed class InfectionConfig : IRoundConfig
{
    /// <summary>
    /// Включён ли раунд «Инфекция» в пуле доступных раундов.
    /// Если <c>false</c>, раунд не будет зарегистрирован и не выпадет.
    /// </summary>
    public bool Enable { get; set; } = true;
    
    public string Name { get; set; } = "Инфекция";

    /// <summary>
    /// Относительный вес раунда при случайном выборе.
    /// Чем больше значение, тем выше шанс выпадения относительно других
    /// включённых раундов (это вес, а не процент).
    /// </summary>
    public int Weight { get; set; } = 20;

    /// <summary>
    /// Разрешено ли возрождение умерших зомби в течение раунда.
    /// Подключившиеся во время раунда игроки возрождаются зомби
    /// независимо от этого параметра.
    /// </summary>
    public bool ZombieRevived { get; set; } = true;

    /// <summary>
    /// Доступен ли рывок (Leap) первому зомби в этом раунде.
    /// </summary>
    public bool FirstZombieLeap { get; set; } = true;

    /// <summary>
    /// Множитель стартового здоровья первого зомби относительно базового HP.
    /// <c>1.0</c> — без изменений, <c>1.5</c> — +50%, <c>0.5</c> — половина.
    /// </summary>
    public float FirstZombieHealthRatio { get; set; } = 1.0f;

    /// <summary>
    /// Задержка в секундах перед возрождением игрока зомби.
    /// Применяется, когда <see cref="ZombieRevived"/> равно <c>true</c>.
    /// </summary>
    public float ZombieSpawnTime { get; set; } = 5.0f;

    /// <summary>
    /// Имя звукового события, проигрываемого при появлении первого заражённого.
    /// Должно совпадать с записью в soundevents (например, "ZombiePlagueAbility.Infection").
    /// </summary>
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Infection";

    /// <summary>
    /// Получает ли первый зомби невидимость в начале раунда.
    /// Длительность задаётся в <see cref="InvisibleDuration"/>.
    /// </summary>
    public bool FirstZombieIsInvisible { get; set; } = true;

    /// <summary>
    /// Длительность невидимости первого зомби в секундах.
    /// Учитывается только при <see cref="FirstZombieIsInvisible"/> = <c>true</c>.
    /// </summary>
    public float InvisibleDuration { get; set; } = 10.0f;
}

public sealed class PlagueConfig : IRoundConfig
{
    /// <summary>
    /// Включён ли раунд «Чума» (массовое заражение) в пуле доступных раундов.
    /// Если <c>false</c>, раунд не будет зарегистрирован и не выпадет.
    /// </summary>
    public bool Enable { get; set; } = true;
    
    public string Name { get; set; } = "Чума";

    /// <summary>
    /// Относительный вес раунда при случайном выборе.
    /// Чем больше значение, тем выше шанс выпадения относительно других
    /// включённых раундов (это вес, а не процент).
    /// </summary>
    public int Weight { get; set; } = 15;

    /// <summary>
    /// Разрешено ли возрождение зомби в течение раунда.
    /// Если <c>true</c>, умершие/подключившиеся игроки возрождаются зомби
    /// (через <see cref="ZombieSpawnTime"/>).
    /// </summary>
    public bool ZombieRevived { get; set; } = true;

    /// <summary>
    /// Доля игроков, заражаемых в начале раунда, от 0 до 1.
    /// Например, <c>0.3</c> — 30% игроков становятся зомби на старте
    /// (число округляется вверх).
    /// </summary>
    public float ZombieSpawnRatio { get; set; } = 0.3f;

    /// <summary>
    /// Задержка в секундах перед возрождением игрока зомби.
    /// Применяется, когда <see cref="ZombieRevived"/> равно <c>true</c>.
    /// </summary>
    public float ZombieSpawnTime { get; set; } = 5.0f;

    /// <summary>
    /// Минимальное количество людей, необходимое для начала раунда.
    /// </summary>
    public int MinimumHumansRequired { get; init; } = 5;
    
    /// <summary>
    /// Проигрывать ли музыку (<see cref="MusicSoundName"/>) в начале раунда.
    /// </summary>
    public bool IsMusicEnabled { get; set; } = true;
    
    /// <summary>
    /// Имя звукового события, проигрываемого в начале раунда.
    /// Учитывается только при <see cref="IsMusicEnabled"/> = <c>true</c>.
    /// </summary>
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Plague";
}

public interface INemesisConfig
{
    /// <summary>
    /// Дополнительное здоровье немезиды за каждого игрока на сервере.
    /// Итоговый бонус = значение × количество игроков.
    /// </summary>
    public int NemesisBonusHealthPerPlayer { get; set; }
}

public interface ISurvivorConfig
{
    /// <summary>
    /// Дополнительное здоровье выжившего за каждого зомби.
    /// Итоговый бонус = значение × количество зомби.
    /// </summary>
    public int SurvivorBonusHealthPerZombie { get; set; }
}

public sealed class NemesisConfig : IRoundConfig, INemesisConfig
{
    /// <summary>
    /// Включён ли раунд «Немезида» в пуле доступных раундов.
    /// Если <c>false</c>, раунд не будет зарегистрирован и не выпадет.
    /// </summary>
    public bool Enable { get; set; } = true;
    
    public string Name { get; set; } = "Немезида";

    /// <summary>
    /// Проигрывать ли музыку (<see cref="MusicSoundName"/>) в начале раунда.
    /// </summary>
    public bool IsMusicEnabled { get; set; } = true;

    /// <summary>
    /// Относительный вес раунда при случайном выборе.
    /// Чем больше значение, тем выше шанс выпадения относительно других
    /// включённых раундов (это вес, а не процент).
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Имя звукового события, проигрываемого в начале раунда.
    /// Учитывается только при <see cref="IsMusicEnabled"/> = <c>true</c>.
    /// </summary>
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Nemesis";

    /// <summary>
    /// Доступен ли рывок (Leap) немезиде.
    /// </summary>
    public bool NemesisLeap { get; set; } = true;

    /// <inheritdoc cref="INemesisConfig.NemesisBonusHealthPerPlayer"/>
    public int NemesisBonusHealthPerPlayer { get; set; } = 1500;

    /// <summary>
    /// Дополнительный урон, наносимый немезидой по людям.
    /// </summary>
    public int NemesisExtraDamage { get; set; } = 125;
    
    /// <summary>
    /// Минимальное количество людей, необходимое для начала раунда.
    /// </summary>
    public int MinimumHumansRequired { get; init; } = 10;
}

public sealed class SurvivorConfig : IRoundConfig, ISurvivorConfig
{
    /// <summary>
    /// Включён ли раунд «Выживший» в пуле доступных раундов.
    /// Если <c>false</c>, раунд не будет зарегистрирован и не выпадет.
    /// </summary>
    public bool Enable { get; set; } = true;
    
    public string Name { get; set; } = "Выживший";

    /// <summary>
    /// Проигрывать ли музыку (<see cref="MusicSoundName"/>) в начале раунда.
    /// </summary>
    public bool IsMusicEnabled { get; set; } = true;

    /// <summary>
    /// Относительный вес раунда при случайном выборе.
    /// Чем больше значение, тем выше шанс выпадения относительно других
    /// включённых раундов (это вес, а не процент).
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// Имя звукового события, проигрываемого в начале раунда.
    /// Учитывается только при <see cref="IsMusicEnabled"/> = <c>true</c>.
    /// </summary>
    public string MusicSoundName { get; set; } = "ZombiePlagueAbility.Survivor";

    /// <inheritdoc cref="ISurvivorConfig.SurvivorBonusHealthPerZombie"/>
    public int SurvivorBonusHealthPerZombie { get; set; } = 150;
    
    /// <summary>
    /// Минимальное количество людей, необходимое для начала раунда.
    /// </summary>
    public int MinimumHumansRequired { get; init; } = 10;
}