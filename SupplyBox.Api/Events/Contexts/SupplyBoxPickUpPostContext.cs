using Common.Hooks.Abstractions;
using SupplyBox.Data;
using SwiftlyS2.Shared.Players;

namespace SupplyBox.Api.Events.Contexts;

/// <summary>
/// Контекст после подбора ящика снабжения.
/// </summary>
public struct SupplyBoxPickUpPostContext(IPlayer player, ISupplyBoxEntity supplyBox) : IPostHookContext
{
    /// <summary>Игрок, подобравший ящик.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Подобранный ящик.</summary>
    public ISupplyBoxEntity SupplyBox { get; set; } = supplyBox;
}
