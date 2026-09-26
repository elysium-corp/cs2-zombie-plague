using Xunit;
using ZombiePlague.Core.Data.Service;

namespace ZombiePlague.Core.Tests;

public sealed class TeamCommandsTests
{
    [Theory]
    [InlineData("jointeam 1", true)]
    [InlineData("  JoinTeam 3", true)]
    [InlineData("teammenu", true)]
    [InlineData("spectate", true)]
    [InlineData("say jointeam", false)]
    [InlineData("jointeamx 1", false)]
    public void RecognisesTeamSelectionCommands(string command, bool expected)
    {
        Assert.Equal(expected, TeamCommands.IsTeamCommand(command));
    }

    [Theory]
    [InlineData("spectate")]
    [InlineData("jointeam 1")]
    [InlineData("jointeam 2")]
    [InlineData("jointeam 3")]
    [InlineData("teammenu")]
    public void PlayersWithoutThePermissionCannotChooseATeam(string command)
    {
        Assert.Equal(TeamCommandAction.Block, TeamCommands.Decide(command, canSpectate: false, isSpectator: false));
        Assert.Equal(TeamCommandAction.Block, TeamCommands.Decide(command, canSpectate: false, isSpectator: true));
    }

    [Theory]
    [InlineData("spectate", false, "MoveToSpectators")]
    [InlineData("jointeam 1", false, "MoveToSpectators")]
    [InlineData("jointeam \"1\"", false, "MoveToSpectators")]
    [InlineData("spectate", true, "Block")]
    [InlineData("jointeam 1", true, "Block")]
    [InlineData("jointeam 2", true, "JoinGame")]
    [InlineData("jointeam 3", true, "JoinGame")]
    [InlineData("jointeam 0", true, "JoinGame")]
    [InlineData("jointeam 2", false, "Block")]
    [InlineData("jointeam 3", false, "Block")]
    [InlineData("jointeam", true, "Block")]
    [InlineData("jointeam 7", true, "Block")]
    [InlineData("teammenu", false, "Allow")]
    public void PermittedPlayersOnlyMoveBetweenSpectatorsAndTheGame(string command, bool isSpectator, string expected)
    {
        Assert.Equal(Enum.Parse<TeamCommandAction>(expected), TeamCommands.Decide(command, canSpectate: true, isSpectator));
    }
}
