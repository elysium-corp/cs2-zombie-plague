using Advertisement.Core.Application;
using CustomHud.Api;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SwiftlyS2.Shared.Players;
using Xunit;

namespace Advertisement.Core.Tests;

public sealed class HudDeliveryTests
{
    [Theory]
    [InlineData("Chat", false, 0)]
    [InlineData("Hud", true, 1)]
    [InlineData("ChatAndHud", false, 1)]
    public void DeliveryModeControlsChatWithoutDuplicatingHud(string mode, bool suppressChat, int calls)
    {
        var api = new Api();
        using var delivery = Create(new() { Mode = Enum.Parse<AdvertisementDeliveryMode>(mode) }, api);
        Assert.Equal(suppressChat, delivery.Send(null!, "Discord", () => "message"));
        Assert.Equal(calls, api.Calls);
    }

    [Fact]
    public void MissingProviderAndRejectedMessageFallBackToChat()
    {
        using var delivery = Create(new() { Mode = AdvertisementDeliveryMode.Hud }, null);
        Assert.False(delivery.Send(null!, "Discord", () => throw new InvalidOperationException("Текст не должен вычисляться")));
        var api = new Api { Accepted = false };
        delivery.Initialize(api);
        Assert.False(delivery.Send(null!, "Discord", () => "message"));
        api.ThrowOnShow = true;
        Assert.False(delivery.Send(null!, "Discord", () => "oversized"));
    }

    [Fact]
    public void PerMessageRuleOverridesPositionAndTimeWithoutChangingOtherMessages()
    {
        var api = new Api();
        using var delivery = Create(new()
        {
            Messages = new(StringComparer.OrdinalIgnoreCase)
            {
                ["Discord"] = new() { Position = HudPosition.TopRight, DurationSeconds = 12 }
            }
        }, api);
        Assert.True(delivery.Send(null!, "discord", () => "message"));
        Assert.Equal(HudPosition.TopRight, api.Last!.Position);
        Assert.Equal(12, api.Last.DurationSeconds);
        Assert.False(delivery.Send(null!, "Rules", () => throw new InvalidOperationException()));
    }

    [Fact]
    public void ReloadAndUnloadClearOnlyAdvertisementChannel()
    {
        var previous = new Api();
        var current = new Api();
        var delivery = Create(new(), previous);
        delivery.Initialize(previous);
        Assert.Empty(previous.Cleared);
        delivery.Initialize(current);
        Assert.Equal(new[] { AdvertisementHudDelivery.Channel }, previous.Cleared);
        delivery.Dispose();
        Assert.Equal(new[] { AdvertisementHudDelivery.Channel }, current.Cleared);
    }

    private static AdvertisementHudDelivery Create(AdvertisementHudConfig config, ICustomHudApi? api)
    {
        var delivery = new AdvertisementHudDelivery(Options.Create(config), NullLogger.Instance);
        delivery.Initialize(api);
        return delivery;
    }

    private sealed class Api : ICustomHudApi
    {
        public bool IsAvailable => true;
        internal bool Accepted = true;
        internal bool ThrowOnShow;
        internal int Calls;
        internal HudMessageOptions? Last;
        internal readonly List<string> Cleared = [];
        public bool Show(IPlayer player, string text, HudMessageOptions? options = null)
        {
            if (ThrowOnShow) throw new ArgumentException("Text too long");
            Calls++; Last = options; return Accepted;
        }
        public int Broadcast(string text, HudMessageOptions? options = null) => throw new NotImplementedException();
        public void Hide(IPlayer player, string channel) => throw new NotImplementedException();
        public void ClearChannel(string channel) => Cleared.Add(channel);
    }
}
