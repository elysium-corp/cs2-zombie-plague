using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст перед выдачей предмета.
/// </summary>
public struct ItemGivePreContext(IPlayer player, IItem item, GiveAction action) : IPreHookContext
{
    /// <summary>Игрок, которому выдаётся предмет.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Выдаваемый предмет.</summary>
    public IItem Item { get; set; } = item;

    /// <summary>Способ выдачи предмета.</summary>
    public GiveAction Action { get; set; } = action;


    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
