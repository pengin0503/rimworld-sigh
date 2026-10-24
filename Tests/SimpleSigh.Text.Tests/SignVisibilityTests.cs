using SimpleSigh.Layout;
using Xunit;

namespace SimpleSigh.Text.Tests;

public class SignVisibilityTests
{
    [Fact]
    public void UnpoweredSignShowsAnyText()
    {
        Assert.True(SignVisibility.ShouldDrawText("研究室", requiresPower: false, hasPowerComp: false, powerOn: false));
    }

    [Fact]
    public void EmptyTextIsNeverDrawn()
    {
        Assert.False(SignVisibility.ShouldDrawText("", false, false, false));
        Assert.False(SignVisibility.ShouldDrawText(null, true, true, true));
    }

    [Fact]
    public void PoweredSignShowsTextOnlyWhenPowerIsOn()
    {
        Assert.True(SignVisibility.ShouldDrawText("LED", requiresPower: true, hasPowerComp: true, powerOn: true));
        Assert.False(SignVisibility.ShouldDrawText("LED", requiresPower: true, hasPowerComp: true, powerOn: false));
    }

    [Fact]
    public void PoweredSignWithoutPowerCompShowsNothing()
    {
        // Misconfigured def: never pretend to be lit.
        Assert.False(SignVisibility.ShouldDrawText("LED", requiresPower: true, hasPowerComp: false, powerOn: true));
    }

    [Fact]
    public void GlowingTextRendersAfterTheLightOverlayButBeforeFog()
    {
        Assert.Equal(3000, SignVisibility.RenderQueueFor(glowsInDark: false));
        Assert.Equal(3150, SignVisibility.RenderQueueFor(glowsInDark: true));
        Assert.InRange(SignVisibility.RenderQueueFor(true), SignVisibility.LightOverlayQueue + 1, SignVisibility.FogOfWarQueue - 1);
        Assert.InRange(SignVisibility.RenderQueueFor(false), SignVisibility.SpriteQueue + 1, SignVisibility.LightOverlayQueue - 1);
    }

    [Theory]
    // CameraZoomRange: Closest 0, Close 1, Middle 2, Far 3, Furthest 4. hideFrom -1 = never hide.
    [InlineData(-1, 0, true)]
    [InlineData(-1, 4, true)]
    [InlineData(3, 2, true)]
    [InlineData(3, 3, false)]
    [InlineData(3, 4, false)]
    [InlineData(2, 1, true)]
    [InlineData(2, 2, false)]
    public void ZoomAllowsTextBelowTheChosenRange(int hideFrom, int currentZoom, bool expected)
    {
        Assert.Equal(expected, SignVisibility.ZoomAllows(currentZoom, hideFrom));
    }
}
