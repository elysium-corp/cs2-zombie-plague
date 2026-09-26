using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using SwiftlyS2.Shared.Players;
using ZombiePlague.Core.Data.Managers.Contracts;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Menus;

namespace ZombiePlague.Core.Data.Service;

internal interface ICommandService : IService;

internal sealed class CommandService(
    ISwiftlyCore core,
    MainMenu mainMenu,
    ZClassMenu zClassMenu,
    HClassMenu hClassMenu,
    AbilityHudMenu abilityHudMenu,
    IPlayerManager playerManager,
    IRoundManager roundManager,
    ISpectatorAccess spectators
) : ICommandService
{
    private Guid _commandHook = Guid.Empty;
    
    public void Register()
    {
        _commandHook = core.Command.HookClientCommand(OnClientCommand);

        mainMenu.RegisterCommands();
        zClassMenu.RegisterCommands();
        hClassMenu.RegisterCommands();
        abilityHudMenu.RegisterCommands();
    }

    public void Unregister()
    {
        mainMenu.UnregisterCommands();
        zClassMenu.UnregisterCommands();
        hClassMenu.UnregisterCommands();
        abilityHudMenu.UnregisterCommands();
        
        core.Command.UnhookClientCommand(_commandHook);
    }
    
    // Стороны назначает режим. Выбор команды доступен только игрокам с правом из SpectatorPermissions,
    // и только для перехода в наблюдатели и возвращения из них.
    private HookResult OnClientCommand(int playerId, string commandLine)
    {
        if (!TeamCommands.IsTeamCommand(commandLine))
        {
            return HookResult.Continue;
        }

        var player = core.PlayerManager.GetPlayer(playerId);

        if (player is not { IsValid: true, IsFakeClient: false })
        {
            return HookResult.Stop;
        }

        var action = TeamCommands.Decide(
            commandLine,
            spectators.CanSpectate(player),
            player.Controller.Team == Team.Spectator);

        switch (action)
        {
            case TeamCommandAction.Allow:
                return HookResult.Continue;

            case TeamCommandAction.MoveToSpectators:
                core.Scheduler.NextWorldUpdate(() => MoveToSpectators(player));
                return HookResult.Stop;

            case TeamCommandAction.JoinGame:
                core.Scheduler.NextWorldUpdate(() => JoinGame(player));
                return HookResult.Stop;

            default:
                return HookResult.Stop;
        }
    }

    private void MoveToSpectators(IPlayer player)
    {
        if (!player.IsValid || player.Controller.Team == Team.Spectator || !spectators.CanSpectate(player))
        {
            return;
        }

        spectators.RememberSpectator(player);

        // Роль снимается до смены команды: гибель при уходе не считается заражением
        // и не запускает возрождение, которое вернуло бы игрока в команду.
        playerManager.Remove(player);
        player.ChangeTeam(Team.Spectator);
    }

    private void JoinGame(IPlayer player)
    {
        if (!player.IsValid || player.Controller.Team != Team.Spectator)
        {
            return;
        }

        spectators.ForgetSpectator(player);

        // Человек входит в CT; во время подготовки он возрождается человеком,
        // в идущем раунде заражения — зомби, в остальных режимах ждёт следующего раунда.
        if (playerManager.TrySetHuman(player))
        {
            roundManager.TryRespawnPlayer(player);
        }
    }
}
