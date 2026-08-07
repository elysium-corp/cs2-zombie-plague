using Menu.Api.Data.Menus;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Commands;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Data.Service.Contracts;

namespace ZombiePlague.Core.Data.Service;

internal interface ICommandService : IService;

internal sealed class CommandService(ISwiftlyCore core) : ICommandService
{
    private Guid _commandHook = Guid.Empty;
    
    public void Register()
    {
        _commandHook = core.Command.HookClientCommand(OnClientCommand);

        MainMenuRegister();
    }

    public void Unregister()
    {
        core.GameEvent.Unhook(_commandHook);
    }
    
    private static HookResult OnClientCommand(int playerId, string commandLine)
    {
        return IsTeamSelectionCommand(commandLine)
            ? HookResult.Stop
            : HookResult.Continue;
    }
    
    private static bool IsTeamSelectionCommand(string commandLine)
    {
        var command = commandLine.AsSpan().TrimStart();
        var separatorIndex = command.IndexOfAny(' ', '\t');

        if (separatorIndex >= 0)
        {
            command = command[..separatorIndex];
        }

        return command.Equals("jointeam", StringComparison.OrdinalIgnoreCase) ||
               command.Equals("teammenu", StringComparison.OrdinalIgnoreCase) ||
               command.Equals("spectate", StringComparison.OrdinalIgnoreCase);
    }

    private void MainMenuRegister()
    {
        HashSet<string> commands = ["menu", "main", "меню", "ьутг", "vty."];

        foreach (var command in commands)
        {
            core.Command.RegisterCommand(
                commandName: command,
                handler: MainMenuHandler,
                registerRaw: true
            );
        }
    }

    private void MainMenuHandler(ICommandContext context)
    {
        var player = context.Sender;

        if (player == null || !player.IsValid)
        {
            return;
        }

        var menu = ZombiePlague.MenuApi.CreateMenu<IMainMenu>();
        
        menu.Open(player);
    }
}