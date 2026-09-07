namespace ZombiePlague.Core.Config.Core;

public sealed class ZombiePlagueCoreConfig
{
    // Включить отталкивание зомби
    public bool KnockbackEnabled { get; set; } = true;

    // Множитель отталкивания зомби в голову
    public float KnockbackHeadMultiply { get; set; } = 2.0f;

    // Множитель отталкивания зомби в тело
    public float KnockbackBodyMultiply { get; set; } = 1.0f;

    // Высота подбрасывания зомби в воздухе после попадания
    public float AirKnockback { get; set; } = 25.0f;

    // Высота подбрасывания зомби на земле после попадания(меньше 150 не работает)
    public float GroundKnockback { get; set; } = 150.0f;

    // Минимальная сила отдачи для отталкивания
    public float MinKnockbackForce { get; set; } = 75.0f;

    // Время до начала заражения
    public int PreStartDelay { get; set; } = 20;

    // Время возрождения зомби
    public int ZombieSpawnDelay { get; set; } = 5;

    // Сколько единиц брони снимает обычный удар зомби
    public int ZombiePrimaryAttackArmorDamage { get; set; } = 1;

    // Сколько единиц брони снимает сильный удар зомби
    public int ZombieSecondaryAttackArmorDamage { get; set; } = 2;

    // Стандартная модель человека
    public string DefaultHumanModel { get; set; } = "characters/models/ctm_sas/ctm_sas.vmdl";

    // Музыка окружения
    public List<string> AmbienceSounds { get; set; } =
        ["ZombiePlague.Ambience.ambience1", "ZombiePlague.Ambience.ambience2", "ZombiePlague.Ambience.ambience3"];

    // Громкость музыки окружения
    public float AmbienceSoundVolume { get; set; } = 0.7f;

    // Музыка проигрываемая до начала раунда
    public List<string> PreparationSounds { get; set; } = ["ZombiePlagueSounds.round_start_2"];

    // Громкость музыки проигрываемой до начала раунда
    public float PreparationSoundVolume { get; set; } = 1.0f;

    // Звук обратного отсчёта
    public string CountdownSound { get; set; } = "ZombiePlagueSounds.countdown";

    // Громкость звука обратного отсчёта
    public float CountdownSoundVolume { get; set; } = 1.0f;

    // Звуки победы людей
    public List<string> HumanWinSounds { get; set; } =
        ["ZombiePlagueSounds.human_win_1", "ZombiePlagueSounds.human_win_2"];

    // Звуки победы зомби
    public List<string> ZombieWinSounds { get; set; } =
        ["ZombiePlagueSounds.zombie_win_1", "ZombiePlagueSounds.zombie_win_2"];

    // Громкость звука победы
    public float WinSoundVolume { get; set; } = 0.7f;
}