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
}
