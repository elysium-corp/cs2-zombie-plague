using CustomEquipment.Data.GameplayItems;
using CustomEquipment.Services;
using Moq;
using SwiftlyS2.Shared.SchemaDefinitions;
using Xunit;

namespace CustomEquipment.Core.Tests;

public sealed class LaserMineSoundServiceTests
{
    [Fact]
    public void DestroyPlaysOnceAndStopsItsGuidAfterConfiguredDuration()
    {
        using var audio = new Audio();
        var settings = Settings with { DestroySoundDuration = 1.7f };
        var sounds = new LaserMineSoundPlayback(settings, audio.Service, () => null);
        sounds.Destroy(default);
        sounds.Destroy(default);
        sounds.Stop();
        sounds.Damage(default);
        Assert.Equal(settings.DestroySound, Assert.Single(audio.Names));
        Assert.Equal(1.7f, Assert.Single(audio.Timers).Delay);
        Assert.Empty(audio.Stopped);

        audio.Timers[0].Callback();
        audio.Timers[0].Callback();
        audio.Service.StopAll();
        Assert.Equal(1u, Assert.Single(audio.Stopped));
    }

    [Fact]
    public void RemovingMineStopsPreparationButLetsExplosionFinishIndependently()
    {
        using var audio = new Audio();
        var model = new Mock<CBaseEntity>();
        model.SetupGet(value => value.IsValidEntity).Returns(true);
        model.SetupGet(value => value.Index).Returns(42u);
        var sounds = new LaserMineSoundPlayback(Settings, audio.Service, () => model.Object);
        var stages = new List<Action>();
        sounds.Start(default, (_, callback) => stages.Add(callback));
        sounds.Destroy(default);
        sounds.Stop();
        foreach (var stage in stages) stage();

        Assert.Equal(new[] { Settings.InstallSound, Settings.DestroySound }, audio.Names);
        Assert.Equal(new[] { 42, -1 }, audio.Sources);
        Assert.Equal(1u, Assert.Single(audio.Stopped));
        Assert.True(audio.Timers[0].Token.IsCancellationRequested);
        Assert.False(audio.Timers[1].Token.IsCancellationRequested);
        audio.Timers[1].Callback();
        Assert.Equal(new uint[] { 1, 2 }, audio.Stopped);
    }

    [Fact]
    public void CleanupStopsAllActiveGuidsAndIgnoresLateCallbacks()
    {
        using var audio = new Audio();
        audio.Service.Play("mine.charge", default, 1f, 42, 1f);
        audio.Service.Play("mine.explosion", default, 1f, -1, 2f);
        audio.Service.StopAll();
        foreach (var timer in audio.Timers)
        {
            Assert.True(timer.Token.IsCancellationRequested);
            timer.Callback();
        }
        Assert.Equal(new uint[] { 1, 2 }, audio.Stopped);
        audio.Service.Play("mine.ready", default, 1f, 43, 1f);
        audio.Service.Dispose();
        audio.Service.Play("mine.explosion", default, 1f, -1, 2f);
        Assert.Equal(new uint[] { 1, 2, 3 }, audio.Stopped);
        Assert.Equal(3, audio.Names.Count);
    }

    private static LaserMineSettings Settings =>
        (LaserMineSettings)GameplayItemDefaults.Get(GameplayItemKeys.LaserMine).Settings;

    private sealed class Audio : IDisposable
    {
        public List<string> Names { get; } = [];
        public List<int> Sources { get; } = [];
        public List<uint> Stopped { get; } = [];
        public List<(float Delay, Action Callback, CancellationTokenSource Token)> Timers { get; } = [];
        public LaserMineSoundService Service { get; }

        public Audio()
        {
            Service = new LaserMineSoundService((name, _, _, source) =>
            {
                Names.Add(name);
                Sources.Add(source);
                return (uint)Names.Count;
            }, Stopped.Add, (delay, callback) =>
            {
                var token = new CancellationTokenSource();
                Timers.Add((delay, callback, token));
                return token;
            });
        }

        public void Dispose()
        {
            Service.Dispose();
            foreach (var timer in Timers) timer.Token.Dispose();
        }
    }
}
