using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст перед выдачей оружия.
/// </summary>
public struct WeaponGivingContext(IPlayer player, IWeapon weapon, GiveAction action) : IPreHookContext
{
    /// <summary>Игрок, которому выдаётся оружие.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Выдаваемое оружие.</summary>
    public IWeapon Weapon { get; set; } = weapon;

    /// <summary>Способ выдачи оружия.</summary>
    public GiveAction Action { get; set; } = action;


    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
