using Common.Hooks.Abstractions;
using SupplyBox.Data;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>
/// Контекст перед выдачей содержимого ящика снабжения игроку.
/// </summary>
public struct SupplyBoxCollectingContext(IPlayer player, ISupplyBoxEntity supplyBox) : IPreHookContext
{
    /// <summary>Игрок, получающий содержимое ящика.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Собираемый ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; set; } = supplyBox;

    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
