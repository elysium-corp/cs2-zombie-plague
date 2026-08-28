using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст после выдачи оружия.
/// </summary>
public readonly struct WeaponGivenContext(IPlayer player, IWeapon weapon, GiveAction action) : IPostHookContext
{
    /// <summary>Игрок, которому выдано оружие.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Выданное оружие.</summary>
    public IWeapon Weapon { get; } = weapon;

    /// <summary>Способ выдачи оружия.</summary>
    public GiveAction Action { get; } = action;
}
