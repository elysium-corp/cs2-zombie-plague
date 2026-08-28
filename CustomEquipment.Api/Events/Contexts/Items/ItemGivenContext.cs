using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст после выдачи предмета.
/// </summary>
public readonly struct ItemGivenContext(IPlayer player, IItem item, GiveAction action) : IPostHookContext
{
    /// <summary>Игрок, которому выдан предмет.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Выданный предмет.</summary>
    public IItem Item { get; } = item;

    /// <summary>Способ выдачи предмета.</summary>
    public GiveAction Action { get; } = action;
}
