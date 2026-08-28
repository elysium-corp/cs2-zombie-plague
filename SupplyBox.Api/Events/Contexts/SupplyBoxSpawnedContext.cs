using Common.Hooks.Abstractions;
using SupplyBox.Data;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>
/// Контекст после успешного создания и регистрации ящика снабжения.
/// </summary>
public readonly struct SupplyBoxSpawnedContext(ISupplyBoxEntity supplyBox) : IPostHookContext
{
    /// <summary>Созданный ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; } = supplyBox;
}
