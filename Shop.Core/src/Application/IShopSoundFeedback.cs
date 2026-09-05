using SwiftlyS2.Shared.Players;

namespace Shop.Core.Application;

/// <summary>Локальное звуковое подтверждение покупки патронов.</summary>
internal interface IShopSoundFeedback
{
    /// <summary>Воспроизводит один из звуков покупки только покупателю.</summary>
    void AmmoPurchased(IPlayer player);

    /// <summary>Сообщает звуком только покупателю, что запас патронов заполнен.</summary>
    void AmmoFull(IPlayer player);
}
