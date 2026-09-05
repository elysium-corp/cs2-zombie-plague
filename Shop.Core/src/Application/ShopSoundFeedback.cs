using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.Sounds;

namespace Shop.Core.Application;

internal sealed class ShopSoundFeedback(ILogger<ShopSoundFeedback> logger) : IShopSoundFeedback
{
    private static readonly string[] PurchaseSounds =
        ["ZombiePlague.ammo_buy_01", "ZombiePlague.ammo_buy_02", "ZombiePlague.ammo_buy_03"];

    /// <inheritdoc />
    public void AmmoPurchased(IPlayer player) =>
        Play(player, PurchaseSounds[Random.Shared.Next(PurchaseSounds.Length)]);

    /// <inheritdoc />
    public void AmmoFull(IPlayer player) => Play(player, "ZombiePlague.cancel");

    private void Play(IPlayer player, string eventName)
    {
        if (!player.IsValid)
        {
            return;
        }

        try
        {
            using var sound = new SoundEvent(eventName)
            {
                SourceEntityIndex = -1,
                Volume = 1f
            };
            sound.Recipients.AddRecipient(player.PlayerID);
            sound.Emit();
        }
        catch (Exception exception)
        {
            // Звуковое подтверждение не должно менять результат уже завершённой покупки.
            logger.LogWarning(exception, "[Shop] Не удалось воспроизвести звук {EventName}.", eventName);
        }
    }
}
