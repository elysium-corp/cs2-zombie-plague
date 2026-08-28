using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст покупки предмета до списания денег.
/// </summary>
public struct ItemPurchasingContext(IPlayer player, IShopItem item) : IPreHookContext
{
    /// <summary>Игрок, совершающий покупку. Может быть заменён обработчиком.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Покупаемый предмет. Может быть заменён обработчиком.</summary>
    public IShopItem Item { get; set; } = item;


    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
