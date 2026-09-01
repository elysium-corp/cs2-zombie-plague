using CustomKnife.Data.Knives;
using CustomKnife.Data.Registrator;
using CustomKnife.Data.Services.Contracts;
using SwiftlyS2.Shared;

namespace CustomKnife.Services;

/// <summary>
/// Возвращает игроков на дефолтный нож после отзыва разрешения
/// либо удаления выбранного ножа из активного каталога.
/// </summary>
internal sealed class KnifeAccessMonitor(
    ISwiftlyCore core,
    IKnivesRegistry knivesRegistry,
    IPlayerKnifeService playerKnifeService,
    IKnifeService knifeService,
    IKnifeAuthorizationService authorizationService)
{
    public void Tick()
    {
        foreach (var player in core.PlayerManager.GetAllPlayers())
        {
            if (!player.IsValid || !player.IsAuthorized || player.IsFakeClient || player.SteamID == 0)
            {
                continue;
            }

            var selectedKnifeId = playerKnifeService.GetKnifeId(player.SteamID);

            if (selectedKnifeId is null || selectedKnifeId == KnifeDefaults.DefaultKnifeId)
            {
                continue;
            }

            if (knivesRegistry.TryGet(selectedKnifeId, out var knife) &&
                authorizationService.CanUse(player, knife))
            {
                continue;
            }

            playerKnifeService.SetKnifeId(player.SteamID, KnifeDefaults.DefaultKnifeId);
            knifeService.TryGiveKnife(player);
        }
    }
}
