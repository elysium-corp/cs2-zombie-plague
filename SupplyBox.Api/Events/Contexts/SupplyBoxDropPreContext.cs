using Common.Hooks.Abstractions;
using SupplyBox.Data;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>
/// Контекст перед попыткой сбросить новый ящик снабжения.
/// </summary>
public struct SupplyBoxDropPreContext(IReadOnlyCollection<ISupplyBoxEntity> activeSupplyBoxes) : IPreHookContext
{
    /// <summary>Снимок уже активных ящиков.</summary>
    public IReadOnlyCollection<ISupplyBoxEntity> ActiveSupplyBoxes { get; } = activeSupplyBoxes;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
