namespace CustomEquipment.Api.Data.Models;

/// <summary>
/// Абсолютные параметры отдачи подкласса оружия. В массивах первый элемент задаёт
/// основной режим, второй — альтернативный; отсутствующие элементы не изменяются.
/// </summary>
public sealed class WeaponRecoil
{
    /// <summary>Сила импульса отдачи. Ноль отключает постоянную составляющую.</summary>
    public IReadOnlyList<float> Magnitude { get; init; } = [];

    /// <summary>Случайное отклонение силы импульса; для отключения отдачи также задайте ноль.</summary>
    public IReadOnlyList<float> MagnitudeVariance { get; init; } = [];

    /// <summary>Направление отдачи в градусах от -180 до 180; ноль — вертикальная отдача.</summary>
    public IReadOnlyList<float> Angle { get; init; } = [];

    /// <summary>Случайное отклонение направления отдачи в градусах от 0 до 180.</summary>
    public IReadOnlyList<float> AngleVariance { get; init; } = [];

    /// <summary>Проверяет размеры массивов и допустимость значений до обращения к движку.</summary>
    public void Validate()
    {
        WeaponHandlingValidation.ValidateModes(Magnitude, nameof(Magnitude));
        WeaponHandlingValidation.ValidateModes(MagnitudeVariance, nameof(MagnitudeVariance));
        WeaponHandlingValidation.ValidateModes(Angle, nameof(Angle), -180f, 180f);
        WeaponHandlingValidation.ValidateModes(AngleVariance, nameof(AngleVariance), 0f, 180f);
    }
}
