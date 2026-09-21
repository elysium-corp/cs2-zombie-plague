namespace CustomEquipment.Api.Data.Models;

public sealed class WeaponFiring
{
    /// <summary>
    /// Базовый разброс пули относительно направления прицела.
    /// 0 = пуля не отклоняется из-за spread.
    /// Значения соответствуют primary/secondary firing mode.
    /// </summary>
    public List<float> Spread { get; init; } = [];

    /// <summary>
    /// Неточность при стрельбе из положения сидя.
    /// 0 = состояние не добавляет отклонение.
    /// </summary>
    public List<float> InaccuracyCrouch { get; init; } = [];

    /// <summary>
    /// Неточность при стрельбе стоя.
    /// 0 = состояние не добавляет отклонение.
    /// </summary>
    public List<float> InaccuracyStand { get; init; } = [];

    /// <summary>
    /// Неточность в прыжке.
    /// </summary>
    public List<float> InaccuracyJump { get; init; } = [];

    /// <summary>
    /// Неточность после приземления.
    /// </summary>
    public List<float> InaccuracyLand { get; init; } = [];

    /// <summary>
    /// Неточность на лестнице.
    /// </summary>
    public List<float> InaccuracyLadder { get; init; } = [];

    /// <summary>
    /// Дополнительная неточность, накапливаемая от выстрелов.
    /// 0 = стрельба сама по себе не увеличивает разброс.
    /// </summary>
    public List<float> InaccuracyFire { get; init; } = [];

    /// <summary>
    /// Неточность при движении.
    /// </summary>
    public List<float> InaccuracyMove { get; init; } = [];

    /// <summary>
    /// Базовый угол отдачи.
    /// 0 = угол отдачи не уводит прицел.
    /// </summary>
    public List<float> RecoilAngle { get; init; } = [];

    /// <summary>
    /// Случайное отклонение угла отдачи.
    /// 0 = угол отдачи детерминирован.
    /// </summary>
    public List<float> RecoilAngleVariance { get; init; } = [];

    /// <summary>
    /// Сила отдачи, уводящая прицел от исходной точки.
    /// 0 = отдача по величине отсутствует.
    /// </summary>
    public List<float> RecoilMagnitude { get; init; } = [];

    /// <summary>
    /// Случайное отклонение силы отдачи.
    /// 0 = сила отдачи детерминирована.
    /// </summary>
    public List<float> RecoilMagnitudeVariance { get; init; } = [];
}
