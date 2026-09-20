using CustomKnife.Data.Knives;
using CustomKnife.Data.Models;
using CustomKnife.Hud;
using Xunit;

namespace CustomKnife.Core.Tests;

public sealed class KnifeHudSelectionTests
{
    [Fact]
    public void OpensPageContainingEquippedKnifeAndRejectsQueuedConfirmationOfAnotherSelection()
    {
        var knives = Catalog(16);
        var view = new KnifeHudSelection();
        Assert.True(view.Bind(knives, "knife_9"));
        Assert.Equal(1, view.Page);
        Assert.Equal(2, view.SelectedSlot);
        Assert.Equal("knife_9", view.Selected!.InternalName);
        Assert.True(view.CanConfirm(2));
        view.Preview(3);
        Assert.False(view.CanConfirm(2));
        Assert.True(view.CanConfirm(3));
    }

    [Fact]
    public void PartialLastPageCannotSelectOrConfirmAnInvisibleSlot()
    {
        var view = new KnifeHudSelection();
        view.Bind(Catalog(9), "knife_0");
        Assert.False(view.Move(-1));
        Assert.True(view.Move(1));
        Assert.Equal(2, view.PageCount);
        Assert.False(view.Move(1));
        Assert.False(view.Preview(2));
        Assert.False(view.CanConfirm(2));
        Assert.Null(view.At(7));
        Assert.Null(view.At(-1));
    }

    [Fact]
    public void ReloadWithSameIdsStillInvalidatesHudAndDeletionClampsPage()
    {
        var view = new KnifeHudSelection();
        var knives = Catalog(16);
        Assert.True(view.Bind(knives, "knife_15"));
        Assert.False(view.Bind(knives.ToArray(), "knife_15"));
        Assert.True(view.Bind(Catalog(16), "knife_15"));
        Assert.Equal(2, view.Page);
        Assert.True(view.Bind(Catalog(3), "knife_0"));
        Assert.Equal(0, view.Page);
        Assert.Equal("knife_0", view.Selected!.InternalName);
        Assert.True(view.Bind([], "knife_0"));
        Assert.Null(view.Selected);
        Assert.False(view.CanConfirm(0));
    }

    [Theory]
    [InlineData("Equip0", true)]
    [InlineData("Equip6", true)]
    [InlineData("Equip7", false)]
    [InlineData("Equip-1", false)]
    [InlineData("Equip01", false)]
    [InlineData("Equip+1", false)]
    [InlineData("Equip 1", false)]
    [InlineData("Equip1extra", false)]
    [InlineData("Preview1", false)]
    public void OnlyCanonicalVisibleButtonIdsAreAccepted(string button, bool accepted)
        => Assert.Equal(accepted, KnifeHudSelection.TrySlot(button, "Equip", out _));

    [Fact]
    public void UnregisteredImageCannotInjectCssClass()
    {
        Assert.Equal("knife", KnifeHudImages.ResolveIcon("../../secret", "knife_axe"));
        Assert.Equal("knife", KnifeHudImages.ResolvePreview("knife Evil", "knife_axe"));
        Assert.Equal("knife", KnifeHudImages.ResolvePreview(null, "knife_axe"));
        Assert.Equal("karambit", KnifeHudImages.ResolveIcon(null, "knife_karambit"));
        Assert.Equal("m9_bayonet", KnifeHudImages.ResolvePreview("m9_bayonet", "knife_axe"));
    }

    private static IKnife[] Catalog(int count) => Enumerable.Range(0, count)
        .Select(index => (IKnife)(KnifeDefaults.Fallback with { InternalName = "knife_" + index }))
        .ToArray();
}
