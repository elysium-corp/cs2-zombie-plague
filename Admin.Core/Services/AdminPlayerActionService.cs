using Admin.Core.Data;
using Localization.Api;
using Microsoft.Extensions.Logging;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Services;

internal sealed class AdminPlayerActionService(ISwiftlyCore core, IPrivilegeService privileges,
    AdminMoneyService money, AdminMovementService movement, CommunicationService communication,
    ILocalizationApi localization, ILogger<AdminPlayerActionService> logger)
{
    private bool _active;
    public void Start() => _active = true;
    public void Stop() => _active = false;

    public bool CanUse(IPlayer? administrator, PlayerAction action) =>
        _active && (administrator is null || administrator is { IsValid: true, IsAuthorized: true } &&
            privileges.HasPermission(administrator.SteamID, PlayerActionPermissions.For(action)));

    public string Text(IPlayer? player, string key, int? amount = null)
    {
        var parameters = amount.HasValue ? new Dictionary<string, string> { ["amount"] = amount.Value.ToString() } : null;
        return player is null ? localization.GetForLanguage("ru", key, parameters) ?? key
            : localization.GetForPlayerOrKey(player, key, parameters);
    }

    public void Execute(IPlayer? administrator, PlayerActionTarget targetReference, PlayerAction action,
        int value, string reason, Action<string> reply)
    {
        if (!CanUse(administrator, action))
        {
            reply(Text(administrator, "Admin.Actions.Denied"));
            return;
        }
        var target = targetReference.Resolve(core);
        if (target is null)
        {
            reply(Text(administrator, "Admin.Actions.TargetMissing"));
            return;
        }
        if (action is PlayerAction.Mute or PlayerAction.Unmute or PlayerAction.Gag or PlayerAction.Ungag)
        {
            if (target is not { IsAuthorized: true, IsFakeClient: false } || target.SteamID == 0)
            {
                reply(Text(administrator, "Admin.Actions.Failed"));
                return;
            }
            var kind = action is PlayerAction.Mute or PlayerAction.Unmute ? CommunicationKind.Mute : CommunicationKind.Gag;
            var actorReference = administrator is null ? (PlayerActionTarget?)null : PlayerActionTarget.From(administrator);
            var actorSteam = administrator?.SteamID;
            var targetSteam = target.SteamID;
            var accepted = communication.Change(targetSteam, kind, actorSteam, value, reason,
                action is PlayerAction.Unmute or PlayerAction.Ungag, success =>
                {
                    if (success) logger.LogInformation("Admin {Administrator} applied {Action} to {Target}, minutes={Minutes}, reason={Reason}",
                        actorSteam, action, targetSteam, value, reason);
                    var actor = actorReference?.Resolve(core);
                    if (actorReference is not null && actor is null) return;
                    reply(Text(actor, success ? "Admin.Actions.Success" : "Admin.Actions.Failed"));
                });
            if (!accepted) reply(Text(administrator, "Admin.Actions.Unavailable"));
            return;
        }

        bool changed;
        var amount = 0;
        switch (action)
        {
            case PlayerAction.Money:
                amount = money.Give(target, value);
                changed = amount > 0;
                break;
            case PlayerAction.Noclip:
                changed = movement.SetNoclip(target, value != 0);
                break;
            case PlayerAction.Grab:
                changed = administrator is not null && movement.Grab(administrator, target);
                break;
            case PlayerAction.Release:
                if (administrator is null) { changed = false; break; }
                movement.Release(administrator);
                changed = true;
                break;
            default:
                changed = false;
                break;
        }
        if (changed) logger.LogInformation("Admin {Administrator} applied {Action} to {Target}, value={Value}, credited={Amount}",
            administrator?.SteamID, action, target.SteamID, value, amount);
        reply(Text(administrator, !changed ? "Admin.Actions.Failed" :
            action == PlayerAction.Money ? "Admin.Actions.MoneyGranted" : "Admin.Actions.Success", amount));
    }
}
