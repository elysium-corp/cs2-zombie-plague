using Admin.Core.Data;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Services;

internal sealed class AdminActionCommands(ISwiftlyCore core, AdminPlayerActionService actions,
    AdminMovementService movement) : IDisposable
{
    private readonly List<Guid> _commands = [];

    public void Start()
    {
        if (_commands.Count != 0) return;
        Register("admin_money", PlayerAction.Money);
        Register("admin_noclip", PlayerAction.Noclip);
        Register("admin_grab", PlayerAction.Grab);
        Register("admin_ungrab", PlayerAction.Release);
        Register("admin_mute", PlayerAction.Mute);
        Register("admin_unmute", PlayerAction.Unmute);
        Register("admin_gag", PlayerAction.Gag);
        Register("admin_ungag", PlayerAction.Ungag);
    }

    private void Register(string name, PlayerAction action) =>
        _commands.Add(core.Command.RegisterCommand(name, context => Execute(context, action), registerRaw: true));

    private void Execute(ICommandContext context, PlayerAction action)
    {
        var actor = context.Sender;
        // Проверяем права из Admin.Core, а не из отдельного permission-каталога Swiftly.
        if (context.IsSentByPlayer && actor is null || !actions.CanUse(actor, action))
        {
            context.Reply(actions.Text(actor, "Admin.Actions.Denied"));
            return;
        }

        IPlayer? target;
        if (action == PlayerAction.Release) target = actor;
        else if (context.Args.Length == 0 && action == PlayerAction.Noclip) target = actor;
        else if (context.Args.Length == 0 && action == PlayerAction.Grab && actor is not null)
        {
            if (movement.HasGrab(actor))
            {
                actions.Execute(actor, PlayerActionTarget.From(actor), PlayerAction.Release, 0, "", context.Reply);
                return;
            }
            target = movement.FindAimTarget(actor);
        }
        else target = context.Args.Length > 0 ? FindTarget(context.Args[0]) : null;

        if (target is null)
        {
            context.Reply(actions.Text(actor, "Admin.Actions.TargetMissing"));
            return;
        }

        var value = action == PlayerAction.Noclip ? (movement.HasNoclip(target) ? 0 : 1) : 30;
        if (action == PlayerAction.Money && context.Args.Length != 2 ||
            context.Args.Length > 1 && !int.TryParse(context.Args[1], out value) ||
            action == PlayerAction.Money && value <= 0 ||
            action == PlayerAction.Noclip && value is not (0 or 1) ||
            action is PlayerAction.Mute or PlayerAction.Gag && (value < 0 || value > 525600))
        {
            context.Reply(actions.Text(actor, "Admin.Actions.InvalidArguments"));
            return;
        }
        var reason = string.Join(' ', context.Args.Skip(2));
        if (reason.Length > 256)
        {
            context.Reply(actions.Text(actor, "Admin.Actions.InvalidArguments"));
            return;
        }
        actions.Execute(actor, PlayerActionTarget.From(target), action, value, reason, context.Reply);
    }

    private IPlayer? FindTarget(string selector)
    {
        var players = core.PlayerManager.GetAllValidPlayers();
        var candidates = selector.StartsWith('#') && int.TryParse(selector.AsSpan(1), out var slot)
            ? players.Where(player => player.PlayerID == slot)
            : ulong.TryParse(selector, out var steamId)
                ? players.Where(player => player.IsAuthorized && player.SteamID == steamId)
                : players.Where(player => string.Equals(player.Controller.PlayerName, selector, StringComparison.OrdinalIgnoreCase));
        var matches = candidates.Take(2).ToArray();
        return matches.Length == 1 ? matches[0] : null;
    }

    public void Dispose()
    {
        foreach (var command in _commands) core.Command.UnregisterCommand(command);
        _commands.Clear();
    }
}
