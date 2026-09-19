namespace ZombiePlague.Core.Config.Ability;

public sealed class AbilityConfig
{
    public HealConfig Heal { get; set; } = new();
    
    public LeapConfig Leap { get; set; } = new();

    public BlindConfig Blind { get; set; } = new();

    public ChargeConfig Charge { get; set; } = new();
    
    public TrapConfig Trap { get; set; } = new();
    
    public CatchConfig Catch { get; set; } = new();
    
    public DoubleJumpConfig DoubleJump { get; set; } = new();
    
    public DisarmConfig Disarm { get; set; } = new();
    
    public FireBombConfig FireBomb { get; set; } = new();
}

public sealed class HealConfig : IAbilityConfig
{
    // Доступна ли способность
    public bool Enable { get; set; } = true;
    
    // Максимальная дистанция до цели для хила 
    public float MaxHealDistance { get; set; } = 350f;

    // Кол-во здоровья, которое будет восстановлено 
    public int HealAmount { get; set; } = 500;
    
    // Время в течение которого способность будет недоступна для повторного применения 
    public float CooldownTime { get; set; } = 20f;

    // Путь к визуальному эффекту способности
    public List<string> ParticleEffectNames { get; set; } = ["particles/kolka/part2.vpcf"];

    // Длительность визуального эффекта
    public float DurationParticleEffect { get; set; } = 2.0f;

    // Путь к звуковому эффекту способности
    public List<string> SoundEffectNames { get; set; } = ["ZombiePlagueAbility.zombie_healing_1"];

    // Эффект после применения способности на таргете (экран покрывается определенным цветом)
    public bool HasScreenEffectAfterAbilityOnTarget { get; set; } = true;

    // Время когда эффект будет появляться и потухать (200 мс тратится на появление эффекта и 200 мс на растворение) 
    public uint DurationEffectAfterAbilityOnTarget { get; set; } = 200;

    // Время сколько эффект задержится на экране (Duration + holdTime + Duration = общее время эффекта на экране)
    public uint HoldTimeEffectAfterAbilityOnTarget { get; set; } = 75;

    // Кол-во красного в цвете эффекта от 0..255
    public byte RedColorEffectAfterAbilityOnTarget { get; set; } = 0;
    
    // Кол-во зеленного в цвете эффекта от 0..255
    public byte GreenColorEffectAfterAbilityOnTarget { get; set; } = 255;
    
    // Кол-во синего в цвете эффекта от 0..255
    public byte BlueColorEffectAfterAbilityOnTarget { get; set; } = 0;
    
    // Прозрачность эффекта от 0..255
    public byte AlphaEffectAfterAbilityOnTarget { get; set; } = 80;
} 

public sealed class LeapConfig : IAbilityConfig
{
    // Доступна ли способность
    public bool Enable { get; set; } = true;
    
    // Время в течение которого способность будет недоступна для повторного применения 
    public float CooldownTime { get; set; } = 10.0f;

    // Максимальная сила прыжка
    public float LeapDistance { get; set; } = 550f;
    
    // Высота прыжка
    public float LeapBoost { get; set; } = 350f;
} 

public sealed class BlindConfig : IAbilityConfig
{
    // Доступна ли способность
    public bool Enable { get; set; } = true;
    
    // Время в течение которого способность будет недоступна для повторного применения 
    public float CooldownTime { get; set; } = 50f;
    
    // Путь к визуальному эффекту способности
    public List<string> ParticleEffectNames { get; set; } = [""];

    // Длительность визуального эффекта
    public float DurationParticleEffect { get; set; } = 2.0f;

    // Путь к звуковому эффекту способности
    public List<string> SoundEffectNames { get; set; } = ["ZombiePlagueAbility.zombie_blind_impact"];

    // Время когда эффект будет появляться и потухать (200 мс тратится на появление эффекта и 200 мс на растворение) 
    public uint DurationEffectAfterAbilityOnAttacker { get; set; } = 300;

    // Время сколько эффект задержится на экране (Duration + holdTime + Duration = общее время эффекта на экране)
    public uint HoldTimeEffectAfterAbilityOnAttacker { get; set; } = 2_000;

    // Кол-во красного в цвете эффекта от 0..255
    public byte RedColorEffectAfterAbilityOnAttacker { get; set; } = 0;
    
    // Кол-во зеленного в цвете эффекта от 0..255
    public byte GreenColorEffectAfterAbilityOnAttacker { get; set; } = 0;
    
    // Кол-во синего в цвете эффекта от 0..255
    public byte BlueColorEffectAfterAbilityOnAttacker { get; set; } = 0;
    
    // Прозрачность эффекта от 0..255
    public byte AlphaEffectAfterAbilityOnAttacker { get; set; } = 255;
}

public sealed class ChargeConfig : IAbilityConfig
{
    // Доступна ли способность
    public bool Enable { get; set; } = true;
    
    // Время в течение которого способность будет недоступна для повторного применения 
    public float CooldownTime { get; set; } = 20f;

    // Путь к визуальному эффекту способности
    public List<string> ParticleEffectNames { get; set; } = [""];

    // Путь к звуковому эффекту способности
    public List<string> SoundEffectNames { get; set; } = ["ZombiePlagueAbility.zombie_pressure"];
    
    // Максимальная скорость при использовании способности
    public float MaxSpeed { get; set; } = 550f;

    // Общее время действия способности 
    public uint ChargeTime { get; set; } = 3;

    // Время обновления скорости (от 0.01 до 0.1, чем меньше, тем плавнее) 
    public float SpeedUpdatePerTimeTick { get; set; } = 0.05f;
}

public sealed class TrapConfig : IAbilityConfig
{
    // Доступна ли способность
    public bool Enable { get; set; } = true;
    
    // Время в течение которого способность будет недоступна для повторного применения 
    public float CooldownTime { get; set; } = 20f;

    // Путь к визуальному эффекту способности
    public string ParticleEffectName { get; set; } = "particles/kolka/part7.vpcf";

    // Путь к звуковому эффекту способности
    public List<string> SoundEffectNames { get; set; } = ["ZombiePlagueAbility.zombie_trap_cathed"];
    
    // Длительность жизни ловушки
    public float LiveDuration { get; set; } = 20f;
    
    // Длительность эффекта ловушки
    public float EffectDuration { get; set; } = 5f;
    
    // Радиус срабатывания ловушки
    public float TriggerRadius { get; set; } = 100f;
    
}

public sealed class CatchConfig : IAbilityConfig
{
    // Доступна ли способность
    public bool Enable { get; set; } = true;
    
    // Время в течение которого способность будет недоступна для повторного применения 
    public float CooldownTime { get; set; } = 20f;
    
    // Путь к звуковому эффекту способности
    public List<string> SoundEffectNames { get; set; } = ["ZombiePlagueAbility.zombie_trap_cathed"];
    
    // Сила притягивание цели к кастеру
    public float Strength { get; set; } = 100f;

    // Максимальная длина луча (дистанция притягивания)
    public float MaxDistance { get; set; } = 1_000f;
    
    // Ширина луча
    public float BeamWidth { get; set; } = 0.5f;
    
    // Кол-во красного в цвете эффекта от 0..255
    public byte RedColorEffect { get; set; } = 255;
    
    // Кол-во зеленного в цвете эффекта от 0..255
    public byte GreenColorEffect { get; set; } = 255;
    
    // Кол-во синего в цвете эффекта от 0..255
    public byte BlueColorEffect { get; set; } = 0;
    
    // Звук промаха
    public string MissSound { get; set; } = "ZombiePlagueAbility.Zombie.Smoker.smoker_miss";
    
    // Звук попадания
    public string ShotSound { get; set; } = "ZombiePlagueAbility.Zombie.Smoker.smoker_drag1";
}

public sealed class DoubleJumpConfig : IAbilityConfig
{
    public bool Enable { get; set; } = true;

    public float BaseJumpUnits { get; set; } = 54f;
}

public sealed class DisarmConfig : IAbilityConfig
{
    public bool Enable { get; set; } = true;

    public float CooldownTime { get; set; } = 3f;

    // Основной летящий снаряд
    public string ParticleProjectileEffectName { get; set; } =
        "particles/kolka/part10_acid2.vpcf";

    // Эффект при запуске
    public string ParticleBurstEffectName { get; set; } =
        "particles/kolka/part10_acid2_burst.vpcf";

    // Эффект попадания
    public string ParticleExplodeEffectName { get; set; } =
        "particles/kolka/part10_acid2_explode.vpcf";

    public float ProjectileSpeed { get; set; } = 600f;

    public float MaxDistance { get; set; } = 1000f;

    public float SpawnOffset { get; set; } = 50f;

    public float UpdateIntervalSeconds { get; set; } = 0.02f;

    // Сила, с которой выброшенное оружие отлетает вперёд
    public float ThrowForce { get; set; } = 650f;

    // Вертикальный импульс выброшенного оружия
    public float ThrowUpForce { get; set; } = 180f;
}

public sealed class FireBombConfig : IAbilityConfig
{
    public bool Enable { get; set; } = true;

    public float CooldownTime { get; set; } = 15f;

    public string ParticleProjectileEffectName { get; set; } =
        "particles/kolka/part10.vpcf";

    public string ParticleExplosionEffectName { get; set; } =
        "particles/kolka/overdrive_buff_start.vpcf";
    
    // Начальная скорость ТОЧНО вдоль прицела.
    public float ProjectileSpeed { get; set; } = 1000f;
    
    // Определяет насколько быстро projectile начнёт падать.
    public float ProjectileGravity { get; set; } = 600f;

    public float MaxDistance { get; set; } = 2000f;

    public float SpawnOffset { get; set; } = 40f;

    public float UpdateIntervalSeconds { get; set; } = 0.02f;

    public float ExplosionRadius { get; set; } = 215f;

    public float KnockbackPower { get; set; } = 400f;

    public float KnockbackUp { get; set; } = 250f;

    public float ExplosionParticleDuration { get; set; } = 1.5f;
    
    public float BurnMaxDamage { get; set; } = 18f;
    
    public float BurnDuration { get; set; } = 3.1f;
}