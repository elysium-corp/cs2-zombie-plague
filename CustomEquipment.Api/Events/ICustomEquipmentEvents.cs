namespace CustomEquipment.Api.Events;

/// <summary>
/// События модуля пользовательского снаряжения.
/// </summary>
public interface ICustomEquipmentEvents
{
    /// <summary>События до выполнения операций.</summary>
    ICustomEquipmentPreEvents Pre { get; }

    /// <summary>События после выполнения операций.</summary>
    ICustomEquipmentPostEvents Post { get; }
}
