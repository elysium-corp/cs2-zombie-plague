using Common.Hooks.Abstractions;
using SupplyBox.Data;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>
/// Контекст перед созданием нового ящика снабжения на карте.
/// </summary>
public struct SupplyBoxSpawningContext(IReadOnlyCollection<ISupplyBoxEntity> activeSupplyBoxes) : IPreHookContext
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
