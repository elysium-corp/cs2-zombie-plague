using Admin.Core.Data;
using Admin.Core.Services;
using SwiftlyS2.Core.Menus.OptionsBase;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;

namespace Admin.Core.Menus;

internal sealed class PlayerActionsMenu(ISwiftlyCore core, AdminPlayerActionService actions)
{
    public void Open(IPlayer administrator, PlayerAction action)
    {
        if (!actions.CanUse(administrator, action)) return;
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(actions.Text(administrator, $"Admin.Actions.{action}"))
            .Design.SetMenuFooterVisible(false).Design.EnableAutoAdjustVisibleItems();
        if (action == PlayerAction.Grab)
        {
            var release = new ButtonMenuOption(actions.Text(administrator, "Admin.Actions.Release"));
            release.Click += (_, args) => Schedule(args.Player, player => Apply(player, PlayerActionTarget.From(player), PlayerAction.Release, 0));
            builder.AddOption(release);
        }
        foreach (var target in core.PlayerManager.GetAllValidPlayers().OrderBy(player => player.Controller.PlayerName))
        {
            var reference = PlayerActionTarget.From(target);
            var option = new ButtonMenuOption(target.Controller.PlayerName);
            option.Click += (_, args) => Schedule(args.Player, player => OpenValues(player, reference, action));
            builder.AddOption(option);
        }
        core.MenusAPI.OpenMenuForPlayer(administrator, builder.Build());
    }

    private void OpenValues(IPlayer administrator, PlayerActionTarget target, PlayerAction action)
    {
        if (!actions.CanUse(administrator, action) || target.Resolve(core) is null) return;
        if (action == PlayerAction.Grab)
        {
            Apply(administrator, target, action, 0);
            return;
        }
        var builder = core.MenusAPI.CreateBuilder()
            .Design.SetMenuTitle(actions.Text(administrator, $"Admin.Actions.{action}"))
            .Design.SetMenuFooterVisible(false).Design.EnableAutoAdjustVisibleItems();
        void Add(string label, PlayerAction selectedAction, int value)
        {
            var option = new ButtonMenuOption(label);
            option.Click += (_, args) => Schedule(args.Player, player => Apply(player, target, selectedAction, value));
            builder.AddOption(option);
        }
        if (action == PlayerAction.Money)
            foreach (var amount in new[] { 1000, 5000, 10000, 16000 }) Add($"+{amount}", action, amount);
        else if (action == PlayerAction.Noclip)
        {
            Add(actions.Text(administrator, "Admin.Actions.Enable"), action, 1);
            Add(actions.Text(administrator, "Admin.Actions.Disable"), action, 0);
        }
        else
        {
            foreach (var minutes in new[] { 5, 30, 60, 1440 })
                Add(actions.Text(administrator, "Admin.Actions.Minutes", minutes), action, minutes);
            Add(actions.Text(administrator, "Admin.Actions.Permanent"), action, 0);
            Add(actions.Text(administrator, "Admin.Actions.Remove"),
                action == PlayerAction.Mute ? PlayerAction.Unmute : PlayerAction.Ungag, 0);
        }
        core.MenusAPI.OpenMenuForPlayer(administrator, builder.Build());
    }

    private void Apply(IPlayer administrator, PlayerActionTarget target, PlayerAction action, int value) =>
        actions.Execute(administrator, target, action, value, "",
            message => administrator.SendMessage(SwiftlyS2.Shared.Players.MessageType.Chat, message));

    private ValueTask Schedule(IPlayer administrator, Action<IPlayer> callback)
    {
        var reference = PlayerActionTarget.From(administrator);
        core.Scheduler.NextTick(() =>
        {
            if (reference.Resolve(core) is { } current) callback(current);
        });
        return ValueTask.CompletedTask;
    }
}
