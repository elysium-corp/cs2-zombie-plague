using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст после успешной покупкой предмета.
/// </summary>
public struct ItemBuyPostContext(IPlayer player, IShopItem item) : IPostHookContext
{
    /// <summary>Игрок, совершивший покупку.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Купленный предмет.</summary>
    public IShopItem Item { get; set; } = item;
}
