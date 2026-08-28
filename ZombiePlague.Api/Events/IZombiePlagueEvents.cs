namespace ZombiePlague.Api.Events;

/// <summary>
/// Публичные события игрового режима Zombie Plague, сгруппированные по доменам.
/// </summary>
public interface IZombiePlagueEvents
{
    /// <summary>События жизненного цикла игроков и их ролей.</summary>
    IZombiePlaguePlayerEvents Players { get; }

    /// <summary>События выбора предпочтительных классов игроков.</summary>
    IZombiePlagueClassEvents Classes { get; }

    /// <summary>События жизненного цикла раундов Zombie Plague.</summary>
    IZombiePlagueRoundEvents Rounds { get; }

    /// <summary>События боевых механик режима.</summary>
    IZombiePlagueCombatEvents Combat { get; }
}
