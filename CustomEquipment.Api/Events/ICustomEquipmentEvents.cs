namespace CustomEquipment.Api.Events;

/// <summary>
/// События модуля пользовательского снаряжения.
/// </summary>
public interface ICustomEquipmentEvents
{
    /// <summary>События покупок и общей выдачи предметов.</summary>
    ICustomEquipmentItemEvents Items { get; }

    /// <summary>События пользовательского оружия.</summary>
    ICustomEquipmentWeaponEvents Weapons { get; }

    /// <summary>События пользовательских гранат.</summary>
    ICustomEquipmentGrenadeEvents Grenades { get; }

    /// <summary>События лазерных мин.</summary>
    ICustomEquipmentMineEvents Mines { get; }
}
