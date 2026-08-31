using Admin.Core.Data;
using Common.Database.Tasks;
using Localization.Api;
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
    DatabaseTaskTracker databaseTasks,
    ILocalizationApi localization) : IBanEnforcementService
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
            BuildKickReason(player, ban),
            ENetworkDisconnectionReason.NETWORK_DISCONNECT_REJECT_BANNED
        ).ConfigureAwait(false);
    }

    private string BuildKickReason(IPlayer player, ActiveBan ban)
    {
        var placeholders = new Dictionary<string, string>
        {
            ["reason"] = ban.Reason,
            ["expires_at"] = ban.ExpiresAtUtc?.ToString("dd.MM.yyyy HH:mm") ?? string.Empty,
        };

        return localization.GetForPlayerOrKey(
            player,
            ban.ExpiresAtUtc is null ? "Admin.Ban.KickPermanent" : "Admin.Ban.KickTemporary",
            placeholders);
    }
}
