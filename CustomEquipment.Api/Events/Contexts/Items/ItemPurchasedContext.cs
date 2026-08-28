using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст покупки после списания денег и постановки выдачи предмета в очередь.
/// </summary>
public readonly struct ItemPurchasedContext(IPlayer player, IShopItem item) : IPostHookContext
{
    /// <summary>Игрок, совершивший покупку.</summary>
    public IPlayer Player { get; } = player;

    /// <summary>Купленный предмет.</summary>
    public IShopItem Item { get; } = item;
}
