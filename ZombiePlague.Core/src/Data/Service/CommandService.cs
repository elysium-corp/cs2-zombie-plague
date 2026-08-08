using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Misc;
using ZombiePlague.Core.Data.Service.Contracts;
using ZombiePlague.Core.Menus;

namespace ZombiePlague.Core.Data.Service;

internal interface ICommandService : IService;

internal sealed class CommandService(
    ISwiftlyCore core,
    MainMenu mainMenu,
    ZClassMenu zClassMenu
) : ICommandService
{
    private Guid _commandHook = Guid.Empty;
    
    public void Register()
    {
        _commandHook = core.Command.HookClientCommand(OnClientCommand);

        mainMenu.RegisterCommands();
        zClassMenu.RegisterCommands();
    }

    public void Unregister()
    {
        mainMenu.UnregisterCommands();
        zClassMenu.UnregisterCommands();
        
        core.Command.UnhookClientCommand(_commandHook);
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
}