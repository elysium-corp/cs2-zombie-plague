using Common.Hooks.Abstractions;
using SupplyBox.Data;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>
/// Контекст после успешной выдачи содержимого ящика снабжения.
/// </summary>
public readonly struct SupplyBoxCollectedContext(IPlayer player, ISupplyBoxEntity supplyBox) : IPostHookContext
{
    /// <summary>Игрок, получивший содержимое ящика.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Собранный ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; } = supplyBox;
}
