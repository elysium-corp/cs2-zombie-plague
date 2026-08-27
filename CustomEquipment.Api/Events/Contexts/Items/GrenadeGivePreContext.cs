using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст перед выдачей гранаты.
/// </summary>
public struct GrenadeGivePreContext(IPlayer player, IGrenade grenade, GiveAction action) : IPreHookContext
{
    /// <summary>Игрок, которому выдаётся граната.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Выдаваемая граната.</summary>
    public IGrenade Grenade { get; set; } = grenade;

    /// <summary>Способ выдачи гранаты.</summary>
    public GiveAction Action { get; set; } = action;


    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
