using Admin.Core.Data;
using Common.Database.Tasks;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.ProtobufDefinitions;

namespace Admin.Core.Services;

/// <summary>
/// Проверяет блокировки авторизованных игроков и отключает
/// игроков с активной блокировкой.
/// </summary>
internal sealed class BanEnforcementService(
    ISwiftlyCore core, 
    IBanService banService, 
    DatabaseTaskTracker databaseTasks) : IBanEnforcementService
{
    /// <inheritdoc />
    public void Check(IPlayer player)
    {
        if (player.IsFakeClient || !player.IsAuthorized || player.SteamID == 0)
        {
            return;
        }

        var playerId = player.PlayerID;
        var steamId = player.SteamID;

        databaseTasks.Run(
            () => CheckAsync(playerId, steamId),
            $"Check ban {steamId}"
        );
    }

    private async Task CheckAsync(int playerId, ulong steamId)
    {
        var ban = await banService.FindActiveAsync(steamId).ConfigureAwait(false);

        if (ban is null)
        {
            return;
        }

        var player = core.PlayerManager.GetPlayer(playerId);

        if (player is null || player.IsFakeClient || player.SteamID != steamId)
        {
            return;
        }

        await player.KickAsync(
            BuildKickReason(ban),
            ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED
        ).ConfigureAwait(false);
    }

    private static string BuildKickReason(ActiveBan ban)
    {
        return ban.ExpiresAtUtc is null
            ? $"Вы заблокированы навсегда. Причина: {ban.Reason}"
            : $"Вы заблокированы до {ban.ExpiresAtUtc:dd.MM.yyyy HH:mm}. Причина: {ban.Reason}";
    }
}