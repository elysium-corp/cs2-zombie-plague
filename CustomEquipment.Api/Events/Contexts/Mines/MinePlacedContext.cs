using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Mines;

/// <summary>
/// Контекст после размещения лазерной мины.
/// </summary>
public readonly struct MinePlacedContext(IPlayer player, LaserMineEntityBase mine) : IPostHookContext
{
    /// <summary>Игрок, разместивший мину.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Размещённая мина.</summary>
    public LaserMineEntityBase Mine { get; } = mine;
}
