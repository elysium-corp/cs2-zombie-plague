using System.Reflection;
using Shop.Core.Hud;
using SwiftlyS2.Shared.Players;

namespace Shop.Core.Tests;

public sealed class ShopHudPresentationTests
{
    [Fact]
    public void PreparedHudDoesNotBlockInputUntilNativeTriggerOrExplicitOpen()
    {
        var presentation = new ShopHudPresentation(true);
        Assert.False(presentation.Visible);
        Assert.False(presentation.IsOpen(false));
        Assert.False(presentation.ObserveNativeTrigger(false));
        Assert.False(presentation.RequestNativeClose(false));

        // Пока сервер ещё не закрепил показ, CSS-триггер уже блокирует покупку патронов.
        Assert.True(presentation.IsOpen(true));
        Assert.False(presentation.Visible);
    }

    [Fact]
    public void NativeCloseKeepsShopVisibleWithoutWaitingForAcknowledgement()
    {
        var presentation = new ShopHudPresentation(true);
        Assert.True(presentation.ObserveNativeTrigger(true));
        presentation.Open(true);
        Assert.True(presentation.Visible);
        Assert.True(presentation.RequestNativeClose(true));

        // Пропавшее подтверждение не прячет магазин и не повторяет buymenu.
        for (var i = 0; i < 100; i++)
        {
            Assert.False(presentation.ObserveNativeTrigger(true));
            Assert.False(presentation.RequestNativeClose(true));
            Assert.True(presentation.Visible);
        }

        Assert.False(presentation.ObserveNativeTrigger(false));
        Assert.True(presentation.Visible);
        Assert.True(presentation.IsOpen(false));
        // Следующее B различимо после закрытия CS2: обработчик закроет открытый Shop.
        Assert.True(presentation.ObserveNativeTrigger(true));
        Assert.True(presentation.Visible);
        Assert.True(presentation.RequestNativeClose(true));
    }

    [Fact]
    public void ExplicitOpenOverNativeMenuConsumesTriggerInsteadOfTogglingShopClosed()
    {
        var presentation = new ShopHudPresentation(true);
        presentation.Open(true);
        Assert.False(presentation.ObserveNativeTrigger(true));
        Assert.True(presentation.RequestNativeClose(true));
        Assert.True(presentation.Visible);
        Assert.False(presentation.ObserveNativeTrigger(false));
        Assert.True(presentation.Visible);
    }

    [Fact]
    public void DisabledReplacementOnlyOpensFromExplicitCommand()
    {
        var presentation = new ShopHudPresentation(false);
        Assert.False(presentation.ObserveNativeTrigger(true));
        Assert.False(presentation.IsOpen(true));
        presentation.Open(false);
        Assert.True(presentation.Visible);
        Assert.True(presentation.IsOpen(false));
    }

    [Fact]
    public void EscapeBeforeServerLatchRejectsLateNativeStateWithoutSendingToggle()
    {
        var presentation = new ShopHudPresentation(true);
        presentation.CancelNativeTrigger();
        Assert.False(presentation.ObserveNativeTrigger(true));
        Assert.False(presentation.RequestNativeClose(true));
        Assert.False(presentation.IsOpen(true));
        Assert.False(presentation.Visible);
        Assert.False(presentation.ObserveNativeTrigger(false));
        Assert.False(presentation.ObserveNativeTrigger(true));
    }

    [Fact]
    public void TrackingUsesLiveTriggerAndDoesNotLeakToReusedPlayerSlot()
    {
        var sessionId = 10UL;
        var nativeOpen = false;
        var reads = 0;
        var player = DispatchProxy.Create<IPlayer, ShopInputTests.InterfaceStub>();
        ((ShopInputTests.InterfaceStub)(object)player).Handler = (method, _) => method.Name switch
        {
            "get_PlayerID" => 4,
            "get_SessionId" => sessionId,
            _ => throw new InvalidOperationException(method.Name)
        };
        var presentation = new ShopHudPresentation(true);
        var state = new ShopHudState();
        state.Track(player, () => { reads++; return presentation.IsOpen(nativeOpen); });
        Assert.False(state.IsOpen(player));
        nativeOpen = true;
        Assert.True(state.IsOpen(player));
        sessionId++;
        Assert.False(state.IsOpen(player));
        Assert.Equal(2, reads);
        state.Close(player.PlayerID);
        sessionId--;
        Assert.False(state.IsOpen(player));
        Assert.Equal(2, reads);
    }
}
