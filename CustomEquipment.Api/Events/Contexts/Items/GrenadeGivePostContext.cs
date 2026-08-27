using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст после выдачи гранаты.
/// </summary>
public struct GrenadeGivePostContext(IPlayer player, IGrenade grenade, GiveAction action) : IPostHookContext
{
    /// <summary>Игрок, которому выдана граната.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Выданная граната.</summary>
    public IGrenade Grenade { get; set; } = grenade;

    /// <summary>Способ выдачи гранаты.</summary>
    public GiveAction Action { get; set; } = action;
}
