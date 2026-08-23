using Statistics.Core.Data;

namespace Statistics.Core.Tests;

public sealed class RoundParticipantStateTests
{
    [Fact]
    public void RoleChangeResetsCurrentStreaksButPreservesRoundBest()
    {
        var participant = new RoundParticipantState();
        participant.SetRole(PlayerRole.Human);
        participant.RecordZombieKill();
        participant.RecordZombieKill();

        participant.SetRole(PlayerRole.Zombie);

        Assert.Equal(0, participant.CurrentKillStreak);
        Assert.Equal(0, participant.CurrentInfectionStreak);
        Assert.Equal(2, participant.BestKillStreak);
    }

    [Fact]
    public void DeathResetsBothCurrentStreaks()
    {
        var participant = new RoundParticipantState();
        participant.SetRole(PlayerRole.Human);
        participant.RecordZombieKill();
        participant.SetRole(PlayerRole.Zombie);
        participant.RecordInfectionMade();

        participant.RecordDeath();

        Assert.Equal(0, participant.CurrentKillStreak);
        Assert.Equal(0, participant.CurrentInfectionStreak);
        Assert.Equal(1, participant.Deaths);
        Assert.Equal(1, participant.BestKillStreak);
        Assert.Equal(1, participant.BestInfectionStreak);
    }

    [Fact]
    public void PointsContextContainsOnlyCurrentRoundValues()
    {
        var participant = new RoundParticipantState();
        participant.SetRole(PlayerRole.Zombie);
        participant.RecordInfectionMade();
        participant.RecordTimesInfected();
        participant.RecordDeath();

        var context = participant.CreatePointsContext(humanWon: false, zombieWon: true);

        Assert.Equal(1, context.InfectionsMade);
        Assert.Equal(1, context.TimesInfected);
        Assert.Equal(1, context.Deaths);
        Assert.False(context.HumanWin);
        Assert.True(context.ZombieWin);
    }
}
