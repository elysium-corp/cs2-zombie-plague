namespace CustomEquipment.Api.Data.Models;

public sealed class WeaponDamage
{
    // - множитель урона по частям тела
    public DamageMultiplier? DamageMultiplier { get; init; }
    
    // - сколько снарядов вылетает за 1 выстрел
    public int NumBullets { get; init; } = 1;
    
    // - пробиваемость поверхностей (MP9 = 1.0, SCAR20 = 2.5), SCAR20 лучше пробивает поверхности и сохраняет урон
    public float? Penetration { get; init; }
    
    // - максимальная эффективная дистанция расчёта урона/полёта в игровых единицах
    // (MP9 = 3600, SCAR20 = 8192)
    public float? Range { get; init; }
    
    // - Коэффициент падения урона на дистанции, чем ближе к 1.0, тем слабее урон падает на расстоянии.
    // (MP9 = 0.87, SCAR20 = 0.98)
    public float? RangeModifier { get; init; }
}

public sealed class DamageMultiplier
{
    // - голова
    public float Head { get; init; } = 1.0f;
    
    // - грудь
    public float Chest { get; init; } = 1.0f;

    // - живот
    public float Stomach { get; init; } = 1.0f;
    
    // - левая/права рука
    public Arm Arms { get; init; } = new Arm();
    
    // - левая/правая нога
    public Leg Legs { get; init; } = new Leg();

    // - шея
    public float Neck { get; init; } = 1.0f;

    public sealed class Arm
    {
        public Arm(float left = 1.0f, float right = 1.0f)
        {
            Left = left;
            Right = right;
        }
        
        public float Left { get; init; }

        public float Right { get; init; }
    }

    public sealed class Leg
    {
        public Leg(float left = 1.0f, float right = 1.0f)
        {
            Left = left;
            Right = right;
        }
        
        public float Left { get; init; }

        public float Right { get; init; }
    }
}