using Common.Hooks.Abstractions;
using SupplyBox.Data;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>
/// Контекст после успешного сброса ящика снабжения.
/// </summary>
public struct SupplyBoxDropPostContext(ISupplyBoxEntity supplyBox) : IPostHookContext
{
    /// <summary>Сброшенный ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; set; } = supplyBox;
}
