using System.Reflection;
using CustomEquipment.Api.Data;
using CustomEquipment.Data.Equipments.Weapons.Equipments.Entities;
using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Services;
using Moq;
using SwiftlyS2.Shared;
using SwiftlyS2.Shared.Players;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMineBeamDamageTests
{
    [Theory]
    [InlineData(false, Team.CT, Team.T)]
    [InlineData(true, Team.CT, Team.CT)]
    [InlineData(true, Team.T, Team.T)]
    public void UnarmedMineFriendsAndInfectedOwnerCannotDealDamage(bool armed, Team ownerTeam, Team targetTeam)
    {
        using var setup = new Setup(armed, ownerTeam, targetTeam);
        setup.Hit();
        setup.TargetPawn.Verify(value => value.TakeDamage(It.IsAny<float>(), It.IsAny<DamageTypes_t>(),
            It.IsAny<CBaseEntity>(), It.IsAny<CBaseEntity>(), It.IsAny<CBaseEntity>()), Times.Never);
    }

    [Fact]
    public void EnemyDamageUsesOwnerPawnAndToleratesMineRemovalInsideDamageHook()
    {
        using var setup = new Setup(true, Team.CT, Team.T);
        setup.TargetPawn.Setup(value => value.TakeDamage(35f, DamageTypes_t.DMG_POISON,
                setup.OwnerPawn.Object, setup.OwnerPawn.Object, null))
            .Callback(() => setup.Mine.Dispose());
        setup.Hit();
        setup.TargetPawn.Verify(value => value.TakeDamage(35f, DamageTypes_t.DMG_POISON,
            setup.OwnerPawn.Object, setup.OwnerPawn.Object, null), Times.Once);
        Assert.Null(setup.Mine.LaserMine);
    }

    private sealed class Setup : IDisposable
    {
        public Mock<CCSPlayerPawn> TargetPawn { get; } = new();
        public Mock<CCSPlayerPawn> OwnerPawn { get; } = new();
        public LaserMineEntity Mine { get; }
        private readonly IPlayer _owner;
        private readonly IPlayer _target;
        private readonly LaserMineSoundService _sounds;

        public Setup(bool armed, Team ownerTeam, Team targetTeam)
        {
            OwnerPawn.SetupGet(value => value.IsValid).Returns(true);
            OwnerPawn.SetupProperty(value => value.Team, ownerTeam);
            TargetPawn.SetupGet(value => value.IsValid).Returns(true);
            TargetPawn.SetupProperty(value => value.Team, targetTeam);
            _owner = Player(OwnerPawn.Object);
            _target = Player(TargetPawn.Object);
            var model = new Mock<CBaseModelEntity>();
            model.SetupGet(value => value.IsValidEntity).Returns(true);
            model.SetupProperty(value => value.Team, Team.CT);
            _sounds = new LaserMineSoundService((_, _, _, _) => throw new InvalidOperationException(
                "Удалённая мина не должна воспроизводить разряд."), _ => { }, (_, _) => new CancellationTokenSource());
            var settings = (LaserMineSettings)GameplayItemDefaults.Get(GameplayItemKeys.LaserMine).Settings;
            Mine = new LaserMineEntity(Mock.Of<ISwiftlyCore>(), settings with { DamagePerTrigger = 35f }, _sounds);
            typeof(LaserMineEntityBase).GetProperty(nameof(LaserMineEntityBase.LaserMine))!.SetValue(Mine, model.Object);
            typeof(LaserMineEntityBase).GetProperty(nameof(LaserMineEntityBase.IsArmed))!.SetValue(Mine, armed);
        }

        public void Hit() => typeof(LaserMineEntity).GetMethod("ApplyDamage", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(Mine, [_target, _owner]);

        public void Dispose()
        {
            Mine.Dispose();
            _sounds.Dispose();
        }

        private static IPlayer Player(CCSPlayerPawn pawn)
        {
            var player = new Mock<IPlayer>();
            player.SetupGet(value => value.IsValid).Returns(true);
            player.SetupGet(value => value.IsAlive).Returns(true);
            player.SetupGet(value => value.PlayerPawn).Returns(pawn);
            return player.Object;
        }
    }
}
