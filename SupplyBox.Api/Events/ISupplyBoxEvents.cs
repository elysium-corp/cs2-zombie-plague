namespace SupplyBox.Api.Events;

/// <summary>
/// События модуля ящиков снабжения.
/// </summary>
public interface ISupplyBoxEvents
{
    /// <summary>События до выполнения операций.</summary>
    ISupplyBoxPreEvents Pre { get; }

    /// <summary>События после выполнения операций.</summary>
    ISupplyBoxPostEvents Post { get; }
}
