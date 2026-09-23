using CustomEquipment.Services;
using Moq;
using SwiftlyS2.Shared.Convars;
using SwiftlyS2.Shared.Players;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class WeaponHandlingPredictionTests
{
    [Fact]
    public void SwitchingModeOrWeaponRestoresCurrentServerValuesWithoutChangingServerConVars()
    {
        var fixture = new Fixture();
        var player = Player();

        fixture.Prediction.Update(player, true, true);
        fixture.Prediction.Update(player, true, true);
        fixture.Recoil.Verify(value => value.ReplicateToClientAsString(1, "0"), Times.Once);
        fixture.Spread.Verify(value => value.ReplicateToClientAsString(1, "1"), Times.Once);

        fixture.Recoil.SetupGet(value => value.ValueAsString).Returns("1.25");
        fixture.Prediction.Update(player, false, true);
        fixture.Recoil.Verify(value => value.ReplicateToClientAsString(1, "1.25"), Times.Once);
        fixture.Spread.Verify(value => value.ReplicateToClientAsString(1, "0"), Times.Never);

        fixture.Prediction.Update(player, false, false);
        fixture.Spread.Verify(value => value.ReplicateToClientAsString(1, "0"), Times.Once);
        fixture.Recoil.VerifySet(value => value.ValueAsString = It.IsAny<string>(), Times.Never);
        fixture.Spread.VerifySet(value => value.ValueAsString = It.IsAny<string>(), Times.Never);
    }

    [Fact]
    public void SlotReuseDoesNotInheritPreviousClientsPredictionState()
    {
        var fixture = new Fixture();
        fixture.Prediction.Update(Player(sessionId: 10), true, false);
        fixture.Prediction.Update(Player(sessionId: 11), true, false);
        fixture.Recoil.Verify(value => value.ReplicateToClientAsString(1, "0"), Times.Exactly(2));

        fixture.Prediction.Remove(1);
        fixture.Prediction.Update(Player(sessionId: 12), true, false);
        fixture.Recoil.Verify(value => value.ReplicateToClientAsString(1, "0"), Times.Exactly(3));
    }

    [Fact]
    public void RestoreOnUnloadOrMapChangeOnlyTouchesClientsWithOverrides()
    {
        var fixture = new Fixture();
        var player = Player();
        fixture.Prediction.Update(player, true, true);
        fixture.Prediction.Restore([player, Player(playerId: 2)]);
        fixture.Prediction.Restore([player]);

        fixture.Recoil.Verify(value => value.ReplicateToClientAsString(1, "2"), Times.Once);
        fixture.Spread.Verify(value => value.ReplicateToClientAsString(1, "0"), Times.Once);
        fixture.Recoil.Verify(value => value.ReplicateToClientAsString(2, It.IsAny<string>()), Times.Never);
        fixture.Spread.Verify(value => value.ReplicateToClientAsString(2, It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void BotsAndDisconnectedClientsDoNotReceivePredictionMessages()
    {
        var fixture = new Fixture();
        fixture.Prediction.Update(Player(isBot: true), true, true);
        fixture.Prediction.Update(Player(isValid: false), true, true);
        fixture.Recoil.VerifyNoOtherCalls();
        fixture.Spread.VerifyNoOtherCalls();
    }

    private static IPlayer Player(int playerId = 1, ulong sessionId = 10, bool isBot = false, bool isValid = true)
    {
        var player = new Mock<IPlayer>(MockBehavior.Strict);
        player.SetupGet(value => value.PlayerID).Returns(playerId);
        player.SetupGet(value => value.SessionId).Returns(sessionId);
        player.SetupGet(value => value.IsValid).Returns(isValid);
        player.SetupGet(value => value.IsFakeClient).Returns(isBot);
        return player.Object;
    }

    private sealed class Fixture
    {
        public Mock<IConVar> Recoil { get; } = new(MockBehavior.Strict);
        public Mock<IConVar> Spread { get; } = new(MockBehavior.Strict);
        public WeaponHandlingPrediction Prediction { get; }

        public Fixture()
        {
            Recoil.SetupGet(value => value.ValueAsString).Returns("2");
            Spread.SetupGet(value => value.ValueAsString).Returns("0");
            Recoil.Setup(value => value.ReplicateToClientAsString(It.IsAny<int>(), It.IsAny<string>()));
            Spread.Setup(value => value.ReplicateToClientAsString(It.IsAny<int>(), It.IsAny<string>()));
            var conVars = new Mock<IConVarService>(MockBehavior.Strict);
            conVars.Setup(value => value.FindAsString("weapon_recoil_scale")).Returns(Recoil.Object);
            conVars.Setup(value => value.FindAsString("weapon_accuracy_nospread")).Returns(Spread.Object);
            Prediction = new WeaponHandlingPrediction(conVars.Object);
        }
    }
}
