using Shop.Core.Hud;

namespace Shop.Core.Tests;

public sealed class ShopHudNativeVisibilityTests
{
    [Fact]
    public void CloseRestoresOnlyAddedBitsAndPreservesConcurrentHudChanges()
    {
        const uint initiallyHidden = (1u << 12) | (1u << 3);
        uint flags = initiallyHidden;
        var visibility = new ShopHudNativeVisibility(() => flags, value => flags = value);
        Assert.Equal(initiallyHidden | ShopHudNativeVisibility.HiddenElements, flags);

        // Во время магазина другой модуль показал здоровье и скрыл чат.
        flags = (flags & ~(1u << 3)) | (1u << 7);
        visibility.Dispose();
        Assert.Equal((1u << 12) | (1u << 7), flags);

        // Повторный Dispose не отменяет изменения после закрытия магазина.
        flags |= 1u << 8;
        visibility.Dispose();
        Assert.Equal((1u << 12) | (1u << 7) | (1u << 8), flags);
    }

    [Fact]
    public void DespawnedPawnIsNeverWrittenDuringDisconnectOrMapCleanup()
    {
        uint? flags = 0;
        var writes = 0;
        var visibility = new ShopHudNativeVisibility(() => flags, value => { flags = value; writes++; });
        Assert.Equal(1, writes);
        flags = null;
        visibility.Dispose();
        Assert.Equal(1, writes);
    }

    [Fact]
    public void AlreadyHiddenElementsRequireNoWritesAndStayHiddenAfterClose()
    {
        var flags = ShopHudNativeVisibility.HiddenElements;
        using var visibility = new ShopHudNativeVisibility(() => flags,
            _ => throw new InvalidOperationException("HUD already hidden"));
    }
}
