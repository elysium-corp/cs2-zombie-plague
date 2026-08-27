using SupplyBox.Api.Events;

namespace SupplyBox;

/// <summary>
/// Общедоступный API ящиков снабжения.
/// </summary>
public interface ISupplyBoxApi
{
    /// <summary>События ящиков снабжения.</summary>
    ISupplyBoxEvents Events { get; }

    /// <summary>Ключ общей регистрации API.</summary>
    static readonly string SharedApiKey = "SupplyBox.Core.ISupplyBoxApi";
}
