using Common.Hooks.Abstractions;
using SupplyBox.Data;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>
/// Контекст перед подбором ящика снабжения.
/// </summary>
public struct SupplyBoxPickUpPreContext(IPlayer player, ISupplyBoxEntity supplyBox) : IPreHookContext
{
    /// <summary>Игрок, подбирающий ящик.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Подбираемый ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; set; } = supplyBox;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
