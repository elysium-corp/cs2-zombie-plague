using Menu.Api.Contracts;
using Menu.Api.Enums;
using Menu.Core.Access;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Menu.Core.Audience;

internal sealed record MenuAudienceResolution(
    bool IsAllowed,
    IReadOnlyList<IPlayer> Targets,
    string? ErrorCode = null)
{
    internal static MenuAudienceResolution Denied(string errorCode) =>
        new(false, Array.Empty<IPlayer>(), errorCode);
}

/// <summary>
/// Разрешает аудиторию из уже загруженного контракта без внешнего I/O.
/// </summary>
internal sealed class MenuAudienceResolver(
    ISwiftlyCore core,
    AdminAccessResolver accessResolver,
    string broadcastPermission)
{
    internal MenuAudienceResolution Resolve(
        IPlayer caller,
        MenuAudienceDefinition? audience,
        IReadOnlyCollection<IPlayer>? explicitTargets = null)
    {
        if (!IsEligible(caller))
        {
            return MenuAudienceResolution.Denied("caller_invalid");
        }

        var effective = audience ?? new MenuAudienceDefinition();
        if (!CanInvoke(caller, effective))
        {
            return MenuAudienceResolution.Denied("broadcast_access_denied");
        }

        IEnumerable<IPlayer> candidates = effective.Kind switch
        {
            MenuAudienceKind.Caller => [caller],
            MenuAudienceKind.AllPlayers => core.PlayerManager.GetAllValidPlayers(),
            MenuAudienceKind.Team => core.PlayerManager.GetInTeam(caller.Controller.Team),
            MenuAudienceKind.AlivePlayers => core.PlayerManager.GetAlive(),
            MenuAudienceKind.DeadPlayers => core.PlayerManager.GetAllValidPlayers().Where(player =>
                !player.IsAlive && player.Controller.Team is Team.T or Team.CT),
            MenuAudienceKind.Spectators => core.PlayerManager.GetSpectators(),
            MenuAudienceKind.ExplicitTargets => ResolveExplicit(effective, explicitTargets),
            _ => Array.Empty<IPlayer>(),
        };

        var targets = candidates
            .Where(IsEligible)
            .GroupBy(player => player.SessionId)
            .Select(group => group.First())
            .ToArray();

        return new MenuAudienceResolution(true, targets);
    }

    internal bool CanInvoke(IPlayer caller, MenuAudienceDefinition? audience)
    {
        if (!IsEligible(caller))
        {
            return false;
        }

        var effective = audience ?? new MenuAudienceDefinition();
        if (effective.Kind == MenuAudienceKind.Caller)
        {
            return true;
        }

        var requiredPermission = string.IsNullOrWhiteSpace(effective.InvokePermission)
            ? broadcastPermission
            : effective.InvokePermission;
        return accessResolver.HasPermission(caller, requiredPermission);
    }

    private IEnumerable<IPlayer> ResolveExplicit(
        MenuAudienceDefinition audience,
        IReadOnlyCollection<IPlayer>? explicitTargets)
    {
        var runtimeTargets = explicitTargets ?? Array.Empty<IPlayer>();
        var configuredTargets = (audience.ExplicitSteamIds ?? Array.Empty<ulong>())
            .Select(steamId => core.PlayerManager.GetPlayerFromSteamId(steamId, allowUnauthorized: false))
            .Where(player => player is not null)
            .Cast<IPlayer>();
        return runtimeTargets.Concat(configuredTargets);
    }

    private static bool IsEligible(IPlayer? player) =>
        player is { IsValid: true, IsAuthorized: true, IsFakeClient: false };
}
