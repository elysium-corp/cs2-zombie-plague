namespace ZombiePlague.Core.Data.Service;

internal enum TeamCommandAction
{
    // Команду не пропускаем: стороны назначает режим.
    Block,

    // Команду выполняет движок, например открывает меню выбора команды.
    Allow,

    // Плагин сам переводит игрока в наблюдатели.
    MoveToSpectators,

    // Плагин сам возвращает наблюдателя в игру.
    JoinGame
}

// Разбор клиентских команд выбора команды: jointeam, teammenu и spectate.
internal static class TeamCommands
{
    private const int SpectatorTeam = 1;

    public static bool IsTeamCommand(string commandLine)
    {
        return Name(commandLine) is "jointeam" or "teammenu" or "spectate";
    }

    public static TeamCommandAction Decide(string commandLine, bool canSpectate, bool isSpectator)
    {
        if (!canSpectate)
        {
            return TeamCommandAction.Block;
        }

        switch (Name(commandLine))
        {
            case "teammenu":
                return TeamCommandAction.Allow;

            case "spectate":
                return isSpectator ? TeamCommandAction.Block : TeamCommandAction.MoveToSpectators;

            case "jointeam" when int.TryParse(Argument(commandLine), out var team):
                if (team == SpectatorTeam)
                {
                    return isSpectator ? TeamCommandAction.Block : TeamCommandAction.MoveToSpectators;
                }

                // Сменить T на CT нельзя даже с правом: сторону определяет роль человека или зомби.
                return isSpectator && team is 0 or 2 or 3 ? TeamCommandAction.JoinGame : TeamCommandAction.Block;

            default:
                return TeamCommandAction.Block;
        }
    }

    private static string Name(string commandLine)
    {
        var command = commandLine.AsSpan().TrimStart();
        var separatorIndex = command.IndexOfAny(' ', '\t');

        return (separatorIndex >= 0 ? command[..separatorIndex] : command).ToString().ToLowerInvariant();
    }

    private static string Argument(string commandLine)
    {
        var parts = commandLine.Split([' ', '\t'], StringSplitOptions.RemoveEmptyEntries);

        return parts.Length > 1 ? parts[1].Trim('"') : "";
    }
}
