using Common.Hooks.Abstractions;
using CustomEquipment.Api.Data.Contracts;
using CustomEquipment.Api.Enums;
using SwiftlyS2.Shared.Players;

namespace CustomEquipment.Api.Events.Contexts.Items;

/// <summary>
/// Контекст перед покупкой предмета.
/// </summary>
public struct ItemBuyPreContext(IPlayer player, IShopItem item) : IPreHookContext
{
    /// <summary>Игрок, совершающий покупку.</summary>
    public IPlayer Player { get; set; } = player;

    /// <summary>Покупаемый предмет.</summary>
    public IShopItem Item { get; set; } = item;


    /// <inheritdoc />
    public bool IsCancelled { get; private set; }

    /// <inheritdoc />
    public void Cancel()
    {
        IsCancelled = true;
    }
}
