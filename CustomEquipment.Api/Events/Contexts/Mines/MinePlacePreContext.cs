using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Mines;

/// <summary>
/// Контекст перед размещением лазерной мины.
/// </summary>
public struct MinePlacePreContext(IPlayer player, LaserMineEntityBase mine) : IPreHookContext
{
    /// <summary>Игрок, размещающий мину.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Размещаемая мина.</summary>
    public LaserMineEntityBase Mine { get; } = mine;


    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
