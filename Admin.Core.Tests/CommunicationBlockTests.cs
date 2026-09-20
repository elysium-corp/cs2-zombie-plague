using Admin.Core.Data;
using Admin.Core.Services;
using Xunit;

namespace Admin.Core.Tests;

public sealed class CommunicationBlockTests
{
    [Fact]
    public void Expiration_ReleasesOnlyExpiredChannel()
    {
        var state = new CommunicationBlockState();
        var now = DateTime.UtcNow;
        state.Set(42, CommunicationKind.Mute, now.AddMinutes(1));
        state.Set(42, CommunicationKind.Gag, null);
        Assert.True(state.IsBlocked(42, CommunicationKind.Mute, now));
        Assert.False(state.IsBlocked(42, CommunicationKind.Mute, now.AddMinutes(1)));
        Assert.True(state.IsBlocked(42, CommunicationKind.Gag, now.AddYears(1)));
    }

    [Fact]
    public void Unmute_DoesNotUngagOrAffectAnotherPlayer()
    {
        var state = new CommunicationBlockState();
        state.Set(42, CommunicationKind.Mute, null);
        state.Set(42, CommunicationKind.Gag, null);
        state.Set(43, CommunicationKind.Mute, null);
        state.Remove(42, CommunicationKind.Mute);
        Assert.False(state.IsBlocked(42, CommunicationKind.Mute, DateTime.UtcNow));
        Assert.True(state.IsBlocked(42, CommunicationKind.Gag, DateTime.UtcNow));
        Assert.True(state.IsBlocked(43, CommunicationKind.Mute, DateTime.UtcNow));
    }

    [Fact]
    public void ReapplyingBlock_ReplacesExpiration()
    {
        var state = new CommunicationBlockState();
        var now = DateTime.UtcNow;
        state.Set(42, CommunicationKind.Mute, now.AddMinutes(1));
        state.Set(42, CommunicationKind.Mute, now.AddHours(1));
        Assert.True(state.IsBlocked(42, CommunicationKind.Mute, now.AddMinutes(2)));
        state.Clear();
        Assert.False(state.IsBlocked(42, CommunicationKind.Mute, now));
    }
}
