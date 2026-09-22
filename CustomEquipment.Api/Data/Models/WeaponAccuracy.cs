namespace CustomEquipment.Api.Data.Models;

/// <summary>
/// Абсолютные параметры разброса подкласса оружия. Массивы содержат до двух значений:
/// основной и альтернативный режим. Пустой массив и null сохраняют параметры движка.
/// </summary>
public sealed class WeaponAccuracy
{
    /// <summary>Базовый случайный разброс пуль в единицах WeaponBaseVData.</summary>
    public IReadOnlyList<float> Spread { get; init; } = [];

    /// <summary>Дополнительная неточность при стрельбе стоя.</summary>
    public IReadOnlyList<float> InaccuracyStand { get; init; } = [];

    /// <summary>Дополнительная неточность при стрельбе сидя.</summary>
    public IReadOnlyList<float> InaccuracyCrouch { get; init; } = [];

    /// <summary>Дополнительная неточность при движении.</summary>
    public IReadOnlyList<float> InaccuracyMove { get; init; } = [];

    /// <summary>Дополнительная неточность от выстрела, накапливаемая при стрельбе очередью.</summary>
    public IReadOnlyList<float> InaccuracyFire { get; init; } = [];

    /// <summary>Дополнительная неточность в прыжке.</summary>
    public IReadOnlyList<float> InaccuracyJump { get; init; } = [];

    /// <summary>Дополнительная неточность при приземлении.</summary>
    public IReadOnlyList<float> InaccuracyLand { get; init; } = [];

    /// <summary>Дополнительная неточность на лестнице.</summary>
    public IReadOnlyList<float> InaccuracyLadder { get; init; } = [];

    /// <summary>Неточность в начале прыжка, общая для обоих режимов.</summary>
    public float? InaccuracyJumpInitial { get; init; }

    /// <summary>Неточность в верхней точке прыжка, общая для обоих режимов.</summary>
    public float? InaccuracyJumpApex { get; init; }

    /// <summary>Неточность от перезарядки, общая для обоих режимов.</summary>
    public float? InaccuracyReload { get; init; }

    /// <summary>Проверяет размеры массивов и допустимость значений до обращения к движку.</summary>
    public void Validate()
    {
        WeaponHandlingValidation.ValidateModes(Spread, nameof(Spread));
        WeaponHandlingValidation.ValidateModes(InaccuracyStand, nameof(InaccuracyStand));
        WeaponHandlingValidation.ValidateModes(InaccuracyCrouch, nameof(InaccuracyCrouch));
        WeaponHandlingValidation.ValidateModes(InaccuracyMove, nameof(InaccuracyMove));
        WeaponHandlingValidation.ValidateModes(InaccuracyFire, nameof(InaccuracyFire));
        WeaponHandlingValidation.ValidateModes(InaccuracyJump, nameof(InaccuracyJump));
        WeaponHandlingValidation.ValidateModes(InaccuracyLand, nameof(InaccuracyLand));
        WeaponHandlingValidation.ValidateModes(InaccuracyLadder, nameof(InaccuracyLadder));
        WeaponHandlingValidation.ValidateValue(InaccuracyJumpInitial, nameof(InaccuracyJumpInitial));
        WeaponHandlingValidation.ValidateValue(InaccuracyJumpApex, nameof(InaccuracyJumpApex));
        WeaponHandlingValidation.ValidateValue(InaccuracyReload, nameof(InaccuracyReload));
    }
}
